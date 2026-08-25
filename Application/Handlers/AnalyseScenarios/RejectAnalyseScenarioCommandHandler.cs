// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for rejecting an AnalyseScenario.
/// Delegates the token-scoped soft-delete (including orphan sub-work /
/// sub-break sweep) to <see cref="IAnalyseScenarioService"/>.
/// When the rejected scenario was a remediation Klacksy prepared, the rejection also flows back onto
/// the condition-ledger row it was prepared for - otherwise the finding would stay Prepared for ever,
/// still pointing at a proposal that has just been thrown away.
/// </summary>
/// <param name="ScenarioId">ID of the scenario to reject</param>

using System.Security.Claims;
using Klacks.Api.Application.Commands.AnalyseScenarios;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Services.Assistant;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Http;

namespace Klacks.Api.Application.Handlers.AnalyseScenarios;

public class RejectAnalyseScenarioCommandHandler : BaseHandler, IRequestHandler<RejectAnalyseScenarioCommand, bool>
{
    private const string LedgerNotRejectedMessage =
        "Condition {ConditionId} was not marked rejected after its prepared scenario {ScenarioId} was rejected; the scenario rejection itself is stored";

    private const string LedgerWriteBackFailedMessage =
        "Writing the rejection of scenario {ScenarioId} back onto its condition failed; the scenario rejection itself is stored";

    private readonly IAnalyseScenarioRepository _repository;
    private readonly IAnalyseScenarioService _scenarioService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWizardRunCaptureRepository _captureRepository;
    private readonly IAgentConditionRepository _conditionRepository;
    private readonly IAgentConditionLedgerService _ledgerService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public RejectAnalyseScenarioCommandHandler(
        IAnalyseScenarioRepository repository,
        IAnalyseScenarioService scenarioService,
        IUnitOfWork unitOfWork,
        IWizardRunCaptureRepository captureRepository,
        IAgentConditionRepository conditionRepository,
        IAgentConditionLedgerService ledgerService,
        IHttpContextAccessor httpContextAccessor,
        ILogger<RejectAnalyseScenarioCommandHandler> logger)
        : base(logger)
    {
        _repository = repository;
        _scenarioService = scenarioService;
        _unitOfWork = unitOfWork;
        _captureRepository = captureRepository;
        _conditionRepository = conditionRepository;
        _ledgerService = ledgerService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<bool> Handle(RejectAnalyseScenarioCommand command, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var scenario = await _repository.Get(command.ScenarioId)
                ?? throw new KeyNotFoundException($"AnalyseScenario with ID {command.ScenarioId} not found");

            await _scenarioService.SoftDeleteScenarioDataAsync(scenario.Token, cancellationToken);

            scenario.Status = AnalyseScenarioStatus.Rejected;
            scenario.RejectReason = command.Reason;
            scenario.RejectReasonText = command.ReasonText;
            await _repository.Put(scenario);
            await _unitOfWork.CompleteAsync();

            var capture = await _captureRepository.GetByScenarioIdAsync(command.ScenarioId, cancellationToken);
            if (capture is not null)
            {
                await _captureRepository.SetOutcomeAsync(capture.Id, CaptureOutcome.Rejected, cancellationToken);
            }

            await RejectLedgerConditionAsync(command.ScenarioId, command.Reason, cancellationToken);

            return true;
        }, nameof(Handle), new { command.ScenarioId });
    }

    /// <summary>
    /// Best-effort write-back onto the finding this scenario was prepared for. Runs AFTER
    /// CompleteAsync, never before: the ledger repository commits in a transaction of its own, and
    /// starting it while the unit of work still holds staged writes would flush them early. Most
    /// scenarios are human-authored and carry no condition at all, which is why a miss is silent.
    /// Not reaching Rejected is an ordinary outcome too - another planner may have dismissed the
    /// finding first, or the tick may have resolved it - so it stays at information level, and the
    /// scenario rejection itself is already durable at this point either way.
    /// </summary>
    private async Task RejectLedgerConditionAsync(
        Guid scenarioId, RejectReason? scenarioReason, CancellationToken cancellationToken)
    {
        try
        {
            var condition = await _conditionRepository.FindByScenarioIdAsync(scenarioId, cancellationToken);
            if (condition is null)
            {
                return;
            }

            var rejected = await _ledgerService.TryRejectAsync(
                condition.Id,
                ConditionRejectReasonMap.FromScenarioRejection(scenarioReason),
                RejectingUserId(),
                cancellationToken);

            if (!rejected)
            {
                _logger.LogInformation(LedgerNotRejectedMessage, condition.Id, scenarioId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, LedgerWriteBackFailedMessage, scenarioId);
        }
    }

    /// <summary>
    /// The rejecting human, read from the request's identity claim. Null when there is no HTTP context
    /// or the claim does not parse as a Guid; the rejection is then recorded without an author, because
    /// who rejected matters less than that the finding was rejected.
    /// </summary>
    private Guid? RejectingUserId()
    {
        var claimValue = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return Guid.TryParse(claimValue, out var userId) ? userId : null;
    }
}
