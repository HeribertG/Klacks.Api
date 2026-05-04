// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for renaming an AnalyseScenario. Updates only the name field,
/// leaving all scenario data (token, dates, status) unchanged.
/// </summary>
/// <param name="Id">ID of the scenario to rename</param>
/// <param name="Name">New name for the scenario</param>

using Klacks.Api.Application.Commands.AnalyseScenarios;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.AnalyseScenarios;

public class RenameAnalyseScenarioCommandHandler : BaseHandler, IRequestHandler<RenameAnalyseScenarioCommand, AnalyseScenarioResource>
{
    private readonly IAnalyseScenarioRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public RenameAnalyseScenarioCommandHandler(
        IAnalyseScenarioRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<RenameAnalyseScenarioCommandHandler> logger)
        : base(logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<AnalyseScenarioResource> Handle(RenameAnalyseScenarioCommand command, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var scenario = await _repository.Get(command.Id)
                ?? throw new KeyNotFoundException($"AnalyseScenario with ID {command.Id} not found");

            scenario.Name = command.Name;

            await _repository.Put(scenario);
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
        }, nameof(Handle), new { command.Id, command.Name });
    }
}
