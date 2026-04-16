// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for rejecting an AnalyseScenario.
/// Delegates the token-scoped soft-delete (including orphan sub-work /
/// sub-break sweep) to <see cref="IAnalyseScenarioService"/>.
/// </summary>
/// <param name="ScenarioId">ID of the scenario to reject</param>

using Klacks.Api.Application.Commands.AnalyseScenarios;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.AnalyseScenarios;

public class RejectAnalyseScenarioCommandHandler : BaseHandler, IRequestHandler<RejectAnalyseScenarioCommand, bool>
{
    private readonly IAnalyseScenarioRepository _repository;
    private readonly IAnalyseScenarioService _scenarioService;
    private readonly IUnitOfWork _unitOfWork;

    public RejectAnalyseScenarioCommandHandler(
        IAnalyseScenarioRepository repository,
        IAnalyseScenarioService scenarioService,
        IUnitOfWork unitOfWork,
        ILogger<RejectAnalyseScenarioCommandHandler> logger)
        : base(logger)
    {
        _repository = repository;
        _scenarioService = scenarioService;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(RejectAnalyseScenarioCommand command, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var scenario = await _repository.Get(command.ScenarioId)
                ?? throw new KeyNotFoundException($"AnalyseScenario with ID {command.ScenarioId} not found");

            await _scenarioService.SoftDeleteScenarioDataAsync(scenario.Token, cancellationToken);

            scenario.Status = AnalyseScenarioStatus.Rejected;
            await _repository.Put(scenario);
            await _unitOfWork.CompleteAsync();

            return true;
        }, nameof(Handle), new { command.ScenarioId });
    }
}
