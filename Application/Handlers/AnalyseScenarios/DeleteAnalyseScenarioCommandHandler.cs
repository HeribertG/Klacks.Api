// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for deleting an AnalyseScenario. Delegates the token-scoped
/// soft-delete (including orphan sub-work / sub-break sweep) to
/// <see cref="IAnalyseScenarioService"/> before removing the scenario row.
/// </summary>
/// <param name="ScenarioId">ID of the scenario to delete</param>

using Klacks.Api.Application.Commands.AnalyseScenarios;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.AnalyseScenarios;

public class DeleteAnalyseScenarioCommandHandler : BaseHandler, IRequestHandler<DeleteAnalyseScenarioCommand, bool>
{
    private readonly IAnalyseScenarioRepository _repository;
    private readonly IAnalyseScenarioService _scenarioService;
    private readonly IWizardRunCaptureRepository _captureRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteAnalyseScenarioCommandHandler(
        IAnalyseScenarioRepository repository,
        IAnalyseScenarioService scenarioService,
        IUnitOfWork unitOfWork,
        IWizardRunCaptureRepository captureRepository,
        ILogger<DeleteAnalyseScenarioCommandHandler> logger)
        : base(logger)
    {
        _repository = repository;
        _scenarioService = scenarioService;
        _captureRepository = captureRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(DeleteAnalyseScenarioCommand command, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var scenario = await _repository.Get(command.ScenarioId)
                ?? throw new KeyNotFoundException($"AnalyseScenario with ID {command.ScenarioId} not found");

            await _scenarioService.SoftDeleteScenarioDataAsync(scenario.Token, cancellationToken);
            await _repository.Delete(command.ScenarioId);
            await _unitOfWork.CompleteAsync();

            var capture = await _captureRepository.GetByScenarioIdAsync(command.ScenarioId, cancellationToken);
            if (capture is not null)
            {
                await _captureRepository.SetOutcomeAsync(capture.Id, CaptureOutcome.Rejected, cancellationToken);
            }

            return true;
        }, nameof(Handle), new { command.ScenarioId });
    }
}
