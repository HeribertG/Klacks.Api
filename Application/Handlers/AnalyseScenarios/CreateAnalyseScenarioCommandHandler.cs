// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler zum Erstellen eines neuen AnalyseScenarios mit eindeutigem Token.
/// </summary>
/// <param name="Request">Enthaelt Name, Beschreibung, Gruppe und Zeitraum</param>

using Klacks.Api.Application.Commands.AnalyseScenarios;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.AnalyseScenarios;

public class CreateAnalyseScenarioCommandHandler : BaseHandler, IRequestHandler<CreateAnalyseScenarioCommand, AnalyseScenarioResource>
{
    private readonly IAnalyseScenarioRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateAnalyseScenarioCommandHandler(
        IAnalyseScenarioRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<CreateAnalyseScenarioCommandHandler> logger)
        : base(logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<AnalyseScenarioResource> Handle(CreateAnalyseScenarioCommand command, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var scenario = new AnalyseScenario
            {
                Name = command.Request.Name,
                Description = command.Request.Description,
                GroupId = command.Request.GroupId,
                FromDate = command.Request.FromDate,
                UntilDate = command.Request.UntilDate,
                Token = Guid.NewGuid()
            };

            await _repository.Add(scenario);
            await _unitOfWork.CompleteAsync();

            return new AnalyseScenarioResource
            {
                Id = scenario.Id,
                Name = scenario.Name,
                Description = scenario.Description,
                GroupId = scenario.GroupId,
                FromDate = scenario.FromDate,
                UntilDate = scenario.UntilDate,
                Token = scenario.Token,
                CreatedByUser = scenario.CreatedByUser,
                Status = (int)scenario.Status
            };
        }, nameof(Handle), new { command.Request.Name, command.Request.GroupId });
    }
}
