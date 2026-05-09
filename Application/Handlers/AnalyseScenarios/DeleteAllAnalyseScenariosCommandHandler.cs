// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for deleting all AnalyseScenarios of a group.
/// Loops through each scenario and delegates token-scoped soft-delete
/// (including orphan sub-work/sub-break sweep) to IAnalyseScenarioService,
/// then saves everything in a single transaction.
/// </summary>
/// <param name="GroupId">Optional group ID to scope the deletion</param>

using Klacks.Api.Application.Commands.AnalyseScenarios;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.AnalyseScenarios;

public class DeleteAllAnalyseScenariosCommandHandler : BaseHandler, IRequestHandler<DeleteAllAnalyseScenariosCommand, bool>
{
    private readonly IAnalyseScenarioRepository _repository;
    private readonly IAnalyseScenarioService _scenarioService;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteAllAnalyseScenariosCommandHandler(
        IAnalyseScenarioRepository repository,
        IAnalyseScenarioService scenarioService,
        IUnitOfWork unitOfWork,
        ILogger<DeleteAllAnalyseScenariosCommandHandler> logger)
        : base(logger)
    {
        _repository = repository;
        _scenarioService = scenarioService;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(DeleteAllAnalyseScenariosCommand command, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var scenarios = await _repository.GetByGroupAsync(command.GroupId, cancellationToken);

            foreach (var scenario in scenarios)
            {
                await _scenarioService.SoftDeleteScenarioDataAsync(scenario.Token, cancellationToken);
                await _repository.Delete(scenario.Id);
            }

            await _unitOfWork.CompleteAsync();

            return true;
        }, nameof(Handle), new { command.GroupId });
    }
}
