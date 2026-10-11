// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Handler for retrieving a single AnalyseScenario by ID; a scenario outside the caller's group visibility is
/// answered like a missing one (null).
/// </summary>
/// <param name="Id">ID of the requested scenario</param>
/// <param name="groupVisibilityGuard">Decides whether the caller may see the scenario's group</param>

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.AnalyseScenarios;
using Klacks.Api.Application.Services.Groups;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.AnalyseScenarios;

public class GetAnalyseScenarioQueryHandler : BaseHandler, IRequestHandler<GetAnalyseScenarioQuery, AnalyseScenarioResource?>
{
    private readonly IAnalyseScenarioRepository _repository;
    private readonly IGroupVisibilityGuard _groupVisibilityGuard;

    public GetAnalyseScenarioQueryHandler(
        IAnalyseScenarioRepository repository,
        IGroupVisibilityGuard groupVisibilityGuard,
        ILogger<GetAnalyseScenarioQueryHandler> logger)
        : base(logger)
    {
        _repository = repository;
        _groupVisibilityGuard = groupVisibilityGuard;
    }

    public async Task<AnalyseScenarioResource?> Handle(GetAnalyseScenarioQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var scenario = await _repository.Get(request.Id);

            if (scenario == null || !await _groupVisibilityGuard.IsScenarioVisibleAsync(scenario, cancellationToken))
            {
                return null;
            }

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
                Status = (int)scenario.Status,
                SubScoreJson = scenario.SubScoreJson,
                ChurnRatio = scenario.ChurnRatio,
                Stage0Violations = scenario.Stage0Violations,
            };
        }, nameof(Handle), new { request.Id });
    }
}
