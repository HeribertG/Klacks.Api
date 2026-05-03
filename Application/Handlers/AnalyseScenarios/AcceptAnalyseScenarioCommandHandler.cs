// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for accepting an AnalyseScenario. Replaces the real schedule
/// data in the scenario's scope (soft-delete via service) and then
/// promotes the scenario rows to real by clearing their AnalyseToken.
/// All data operations go through <see cref="IAnalyseScenarioService"/>.
/// </summary>
/// <param name="ScenarioId">ID of the scenario to accept</param>

using Klacks.Api.Application.Commands.AnalyseScenarios;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.AnalyseScenarios;

public class AcceptAnalyseScenarioCommandHandler : BaseHandler, IRequestHandler<AcceptAnalyseScenarioCommand, bool>
{
    private readonly IAnalyseScenarioRepository _repository;
    private readonly IAnalyseScenarioService _scenarioService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWorkSofteningRepository _softeningRepository;

    public AcceptAnalyseScenarioCommandHandler(
        IAnalyseScenarioRepository repository,
        IAnalyseScenarioService scenarioService,
        IUnitOfWork unitOfWork,
        IWorkSofteningRepository softeningRepository,
        ILogger<AcceptAnalyseScenarioCommandHandler> logger)
        : base(logger)
    {
        _repository = repository;
        _scenarioService = scenarioService;
        _unitOfWork = unitOfWork;
        _softeningRepository = softeningRepository;
    }

    public async Task<bool> Handle(AcceptAnalyseScenarioCommand command, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var scenario = await _repository.Get(command.ScenarioId)
                ?? throw new KeyNotFoundException($"AnalyseScenario with ID {command.ScenarioId} not found");

            await _scenarioService.SoftDeleteRealScheduleDataAsync(scenario.GroupId, scenario.FromDate, scenario.UntilDate, cancellationToken);
            await _scenarioService.PromoteScenarioDataAsync(scenario.Token, cancellationToken);

            await _softeningRepository.DeleteByAnalyseTokenAsync(scenario.Token, cancellationToken);
            await _softeningRepository.DeleteByRangeAndTokenAsync(scenario.FromDate, scenario.UntilDate, null, cancellationToken);

            scenario.Status = AnalyseScenarioStatus.Accepted;
            await _repository.Put(scenario);
            await _unitOfWork.CompleteAsync();

            return true;
        }, nameof(Handle), new { command.ScenarioId });
    }
}
