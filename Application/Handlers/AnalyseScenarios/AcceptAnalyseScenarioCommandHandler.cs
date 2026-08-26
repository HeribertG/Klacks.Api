// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for accepting an AnalyseScenario. Runs the read-only end-state compliance gate BEFORE
/// any mutation: when the scenario would newly introduce Block-mode enforced violations into the
/// real plan and the caller holds no authorised supervisor override, the accept is refused with a
/// conflict. Otherwise it validates the scenario can be applied without conflicting concurrent
/// real-side changes, soft-deletes the real schedule data in the scenario's scope, then promotes
/// scenario works/breaks/schedule-notes to real with shift-id remapping back to the original
/// source shifts. Clone shifts/preferences/shift-expenses are soft-deleted (NOT promoted), so
/// original shift definitions remain untouched and stable across multi-user use. After the
/// promote the real-plan error list is refreshed for the scenario's date range.
/// When the accepted scenario was a remediation Klacksy prepared, the acceptance also flows back onto
/// the condition-ledger row it was prepared for: the finding moves Prepared to Executed and records who
/// released it. Without that write-back a released remediation would leave the finding Prepared for
/// ever, still pointing at a proposal that has already been applied to the real plan.
/// </summary>
/// <param name="ScenarioId">ID of the scenario to accept</param>

using System.Globalization;
using System.Security.Claims;
using Klacks.Api.Application.Commands.AnalyseScenarios;
using Klacks.Api.Application.DTOs.PeriodClosing;
using Klacks.Api.Application.Exceptions;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Http;

namespace Klacks.Api.Application.Handlers.AnalyseScenarios;

public class AcceptAnalyseScenarioCommandHandler : BaseHandler, IRequestHandler<AcceptAnalyseScenarioCommand, bool>
{
    private const string UnknownUserName = "Unknown";
    private const string BlockedMessageFormat =
        "Scenario acceptance blocked: {0} compliance violation(s) enforced in Block mode ({1}). Request a supervisor override to accept anyway.";

    private const string LedgerNotExecutedMessage =
        "Condition {ConditionId} was not marked executed after its prepared scenario {ScenarioId} was accepted; the scenario acceptance itself is stored";

    private const string LedgerWriteBackFailedMessage =
        "Writing the acceptance of scenario {ScenarioId} back onto its condition failed; the scenario acceptance itself is stored";

    private const string AcceptedDetailFormat = "accepted scenario {0}";

    private readonly IAnalyseScenarioRepository _repository;
    private readonly IAnalyseScenarioService _scenarioService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWorkSofteningRepository _softeningRepository;
    private readonly IScenarioComplianceService _complianceService;
    private readonly ISupervisorOverrideAuthorizer _overrideAuthorizer;
    private readonly IScheduleTimelineService _timelineService;
    private readonly IAgentConditionRepository _conditionRepository;
    private readonly IAgentConditionLedgerService _ledgerService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AcceptAnalyseScenarioCommandHandler(
        IAnalyseScenarioRepository repository,
        IAnalyseScenarioService scenarioService,
        IUnitOfWork unitOfWork,
        IWorkSofteningRepository softeningRepository,
        IScenarioComplianceService complianceService,
        ISupervisorOverrideAuthorizer overrideAuthorizer,
        IScheduleTimelineService timelineService,
        IAgentConditionRepository conditionRepository,
        IAgentConditionLedgerService ledgerService,
        IHttpContextAccessor httpContextAccessor,
        ILogger<AcceptAnalyseScenarioCommandHandler> logger)
        : base(logger)
    {
        _repository = repository;
        _scenarioService = scenarioService;
        _unitOfWork = unitOfWork;
        _softeningRepository = softeningRepository;
        _complianceService = complianceService;
        _overrideAuthorizer = overrideAuthorizer;
        _timelineService = timelineService;
        _conditionRepository = conditionRepository;
        _ledgerService = ledgerService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<bool> Handle(AcceptAnalyseScenarioCommand command, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var scenario = await _repository.Get(command.ScenarioId)
                ?? throw new KeyNotFoundException($"AnalyseScenario with ID {command.ScenarioId} not found");

            await _scenarioService.ValidateNoAcceptConflictsAsync(scenario.Token, cancellationToken);

            await EnforceComplianceGateAsync(scenario.FromDate, scenario.UntilDate, scenario.GroupId, scenario.Token, command.OverrideBlock, cancellationToken);

            await _scenarioService.SoftDeleteRealScheduleDataAsync(scenario.GroupId, scenario.Token, scenario.FromDate, scenario.UntilDate, cancellationToken);
            await _scenarioService.PromoteScenarioWorksAsync(scenario.Token, scenario.FromDate, scenario.UntilDate, cancellationToken);

            await _softeningRepository.DeleteByAnalyseTokenAsync(scenario.Token, cancellationToken);
            await _softeningRepository.DeleteByRangeAndTokenAsync(scenario.FromDate, scenario.UntilDate, null, cancellationToken);

            scenario.Status = AnalyseScenarioStatus.Accepted;
            await _repository.Put(scenario);
            await _unitOfWork.CompleteAsync();

            _timelineService.QueueRangeCheck(scenario.FromDate, scenario.UntilDate, null);

            await ExecuteLedgerConditionAsync(command.ScenarioId, cancellationToken);

            return true;
        }, nameof(Handle), new { command.ScenarioId });
    }

    /// <summary>
    /// Best-effort write-back onto the finding this scenario was prepared for. Runs AFTER CompleteAsync,
    /// never before, for two independent reasons. The ledger repository is self-committing: it calls
    /// SaveChanges on the SAME DbContext this handler stages into, so running it first would flush this
    /// handler's still-unfinished writes along with its own - the mixing that .claude/rules/backend-
    /// architecture.md forbids (opening a transaction is not what would flush them; that SaveChanges is).
    /// It also opens that transaction itself and therefore must not run inside an ambient one, which
    /// IAgentConditionRepository.TryTransitionAsync documents. Most scenarios are human-authored and
    /// carry no condition at all, which is why a miss is silent. Not reaching Executed is an ordinary
    /// outcome too - another planner may have dismissed the finding first, or the tick may have resolved it
    /// - so it stays at information level, and the acceptance itself is already durable at this point
    /// either way. Nothing here may throw: the plan has been promoted to the real schedule by now, and
    /// failing the request over a bookkeeping write would tell the user their accept did not happen.
    /// </summary>
    private async Task ExecuteLedgerConditionAsync(Guid scenarioId, CancellationToken cancellationToken)
    {
        try
        {
            var condition = await _conditionRepository.FindByScenarioIdAsync(scenarioId, cancellationToken);
            if (condition is null)
            {
                return;
            }

            var acceptingUserId = AcceptingUserId();

            var executed = await _ledgerService.TryTransitionAsync(
                condition.Id,
                AgentConditionStatus.Prepared,
                AgentConditionStatus.Executed,
                acceptingUserId,
                string.Format(CultureInfo.InvariantCulture, AcceptedDetailFormat, scenarioId),
                new AgentConditionTransitionFields(
                    HandlingKind: AgentConditionHandlingKind.Executed,
                    ApprovedByUserId: acceptingUserId),
                cancellationToken);

            if (!executed)
            {
                _logger.LogInformation(LedgerNotExecutedMessage, condition.Id, scenarioId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, LedgerWriteBackFailedMessage, scenarioId);
        }
    }

    /// <summary>
    /// The accepting human, read from the request's identity claim. Null when there is no HTTP context or
    /// the claim does not parse as a Guid; the acceptance is then recorded without an author rather than
    /// abandoned, mirroring how RejectAnalyseScenarioCommandHandler treats the same case.
    /// </summary>
    private Guid? AcceptingUserId()
    {
        var claimValue = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return Guid.TryParse(claimValue, out var userId) ? userId : null;
    }

    /// <summary>
    /// Read-only accept gate: evaluates the end-state compliance diff of the scenario versus the
    /// real plan BEFORE any mutation. Blocking issues refuse the accept unless the caller holds an
    /// authorised supervisor override, which is audit-logged.
    /// </summary>
    private async Task EnforceComplianceGateAsync(
        DateOnly fromDate,
        DateOnly untilDate,
        Guid? groupId,
        Guid token,
        bool overrideBlockRequested,
        CancellationToken cancellationToken)
    {
        var report = await _complianceService.EvaluateAsync(fromDate, untilDate, groupId, token, cancellationToken);
        if (report.BlockingIssues.Count == 0)
        {
            return;
        }

        var blockedRules = ResolveBlockedRules(report.BlockingIssues);

        var authorized = await _overrideAuthorizer.IsAuthorizedAsync(overrideBlockRequested);
        if (!authorized)
        {
            throw new ConflictException(string.Format(
                BlockedMessageFormat,
                report.BlockingIssues.Count,
                string.Join(", ", blockedRules)));
        }

        LogOverride(blockedRules, report.BlockingIssues.Count);
    }

    private static List<string> ResolveBlockedRules(IReadOnlyList<PeriodIssueDto> blockingIssues)
        => blockingIssues
            .Select(issue => issue.MessageParams.TryGetValue(ComplianceRuleNames.EnforcementRuleParamKey, out var rule)
                ? rule
                : issue.Code)
            .Distinct()
            .ToList();

    private void LogOverride(IReadOnlyList<string> blockedRules, int issueCount)
    {
        var userName = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? UnknownUserName;

        _logger.LogWarning(
            "Compliance override: user {User} overrode {RuleCount} blocked rule(s) ({Rules}) to accept a scenario with {IssueCount} blocking issue(s).",
            userName,
            blockedRules.Count,
            string.Join(",", blockedRules),
            issueCount);
    }
}
