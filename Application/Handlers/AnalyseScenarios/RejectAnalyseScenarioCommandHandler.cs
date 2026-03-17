// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler zum Ablehnen eines AnalyseScenarios.
/// </summary>
/// <param name="ScenarioId">ID des abzulehnenden Szenarios</param>

using Klacks.Api.Application.Commands.AnalyseScenarios;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.AnalyseScenarios;

public class RejectAnalyseScenarioCommandHandler : BaseHandler, IRequestHandler<RejectAnalyseScenarioCommand, bool>
{
    private readonly IAnalyseScenarioRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public RejectAnalyseScenarioCommandHandler(
        IAnalyseScenarioRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<RejectAnalyseScenarioCommandHandler> logger)
        : base(logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(RejectAnalyseScenarioCommand command, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var scenario = await _repository.Get(command.ScenarioId);

            if (scenario == null)
            {
                throw new KeyNotFoundException($"AnalyseScenario with ID {command.ScenarioId} not found");
            }

            // TODO: Implement reject logic - delete all data with this token
            scenario.Status = AnalyseScenarioStatus.Rejected;
            await _repository.Put(scenario);
            await _unitOfWork.CompleteAsync();

            return true;
        }, nameof(Handle), new { command.ScenarioId });
    }
}
