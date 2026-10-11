// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Handler for renaming an AnalyseScenario. Updates only the name field,
/// leaving all scenario data (token, dates, status) unchanged.
/// </summary>
/// <param name="Id">ID of the scenario to rename</param>
/// <param name="Name">New name for the scenario</param>
/// <param name="groupVisibilityGuard">Answers a scenario outside the caller's group visibility like a missing one</param>

using Klacks.Api.Application.Commands.AnalyseScenarios;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services.Groups;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.AnalyseScenarios;

public class RenameAnalyseScenarioCommandHandler : BaseHandler, IRequestHandler<RenameAnalyseScenarioCommand, AnalyseScenarioResource>
{
    private readonly IAnalyseScenarioRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IGroupVisibilityGuard _groupVisibilityGuard;

    public RenameAnalyseScenarioCommandHandler(
        IAnalyseScenarioRepository repository,
        IUnitOfWork unitOfWork,
        IGroupVisibilityGuard groupVisibilityGuard,
        ILogger<RenameAnalyseScenarioCommandHandler> logger)
        : base(logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _groupVisibilityGuard = groupVisibilityGuard;
    }

    public async Task<AnalyseScenarioResource> Handle(RenameAnalyseScenarioCommand command, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var scenario = await _groupVisibilityGuard.EnsureScenarioVisibleAsync(
                await _repository.Get(command.Id), command.Id, cancellationToken);

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
                RunGroupId = scenario.RunGroupId,
                CreatedByUser = scenario.CreatedByUser,
                Status = (int)scenario.Status
            };
        }, nameof(Handle), new { command.Id, command.Name });
    }
}
