// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for creating a new AnalyseScenario. Stores the scenario row and
/// delegates the full schedule clone (shifts, works with ParentWorkId
/// mapping, work_changes, expenses, breaks, schedule_notes) to
/// <see cref="IAnalyseScenarioService"/>.
/// </summary>
/// <param name="Request">Contains name, description, optional group and time period</param>

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
    private readonly IAnalyseScenarioService _scenarioService;
    private readonly IUnitOfWork _unitOfWork;

    public CreateAnalyseScenarioCommandHandler(
        IAnalyseScenarioRepository repository,
        IAnalyseScenarioService scenarioService,
        IUnitOfWork unitOfWork,
        ILogger<CreateAnalyseScenarioCommandHandler> logger)
        : base(logger)
    {
        _repository = repository;
        _scenarioService = scenarioService;
        _unitOfWork = unitOfWork;
    }

    public async Task<AnalyseScenarioResource> Handle(CreateAnalyseScenarioCommand command, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var token = Guid.NewGuid();
            var fromDate = command.Request.FromDate;
            var untilDate = command.Request.UntilDate;
            var groupId = command.Request.GroupId;

            var scenario = new AnalyseScenario
            {
                Name = command.Request.Name,
                Description = command.Request.Description,
                GroupId = groupId,
                FromDate = fromDate,
                UntilDate = untilDate,
                Token = token
            };

            await _repository.Add(scenario);
            await _scenarioService.CloneScenarioDataAsync(groupId, fromDate, untilDate, token, cancellationToken);
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
