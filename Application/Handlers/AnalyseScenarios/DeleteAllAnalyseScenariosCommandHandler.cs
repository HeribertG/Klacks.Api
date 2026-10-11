// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Handler for deleting all AnalyseScenarios of a group.
/// Loops through each scenario and delegates token-scoped soft-delete
/// (including orphan sub-work/sub-break sweep) to IAnalyseScenarioService,
/// then saves everything in a single transaction.
/// </summary>
/// <param name="GroupId">Group whose scenarios are deleted; null deletes the group-less scenarios</param>
/// <param name="groupVisibilityGuard">Refuses a hidden group like a missing one and group-less scenarios for restricted callers</param>

using Klacks.Api.Application.Commands.AnalyseScenarios;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services.Groups;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.AnalyseScenarios;

public class DeleteAllAnalyseScenariosCommandHandler : BaseHandler, IRequestHandler<DeleteAllAnalyseScenariosCommand, bool>
{
    private readonly IAnalyseScenarioRepository _repository;
    private readonly IAnalyseScenarioService _scenarioService;
    private readonly IWizardRunCaptureRepository _captureRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IGroupVisibilityGuard _groupVisibilityGuard;

    public DeleteAllAnalyseScenariosCommandHandler(
        IAnalyseScenarioRepository repository,
        IAnalyseScenarioService scenarioService,
        IUnitOfWork unitOfWork,
        IWizardRunCaptureRepository captureRepository,
        IGroupVisibilityGuard groupVisibilityGuard,
        ILogger<DeleteAllAnalyseScenariosCommandHandler> logger)
        : base(logger)
    {
        _repository = repository;
        _scenarioService = scenarioService;
        _captureRepository = captureRepository;
        _unitOfWork = unitOfWork;
        _groupVisibilityGuard = groupVisibilityGuard;
    }

    public async Task<bool> Handle(DeleteAllAnalyseScenariosCommand command, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            await _groupVisibilityGuard.EnsureScenarioScopeWritableAsync(command.GroupId, cancellationToken);

            var scenarios = await _repository.GetByGroupAsync(command.GroupId, cancellationToken);

            foreach (var scenario in scenarios)
            {
                await _scenarioService.SoftDeleteScenarioDataAsync(scenario.Token, cancellationToken);
                await _repository.Delete(scenario.Id);
            }

            await _unitOfWork.CompleteAsync();

            foreach (var scenario in scenarios)
            {
                var capture = await _captureRepository.GetByScenarioIdAsync(scenario.Id, cancellationToken);
                if (capture is not null)
                {
                    await _captureRepository.SetOutcomeAsync(capture.Id, CaptureOutcome.Rejected, cancellationToken);
                }
            }

            return true;
        }, nameof(Handle), new { command.GroupId });
    }
}
