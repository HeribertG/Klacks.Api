// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Handler for the live summary of one AnalyseScenario; returns null when the scenario does not exist or lies
/// outside the caller's group visibility.
/// </summary>
/// <param name="repository">Resolves the scenario by its ID</param>
/// <param name="summaryBuilder">Computes the summary from the scenario's own shifts and works</param>
/// <param name="groupVisibilityGuard">Decides whether the caller may see the scenario's group</param>

using Klacks.Api.Application.DTOs.Schedules.Summary;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Application.Queries.AnalyseScenarios;
using Klacks.Api.Application.Services.Groups;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.AnalyseScenarios;

public class GetScenarioSummaryQueryHandler : BaseHandler, IRequestHandler<GetScenarioSummaryQuery, ScenarioSummaryDto?>
{
    private readonly IAnalyseScenarioRepository _repository;
    private readonly IScenarioSummaryBuilder _summaryBuilder;
    private readonly IGroupVisibilityGuard _groupVisibilityGuard;

    public GetScenarioSummaryQueryHandler(
        IAnalyseScenarioRepository repository,
        IScenarioSummaryBuilder summaryBuilder,
        IGroupVisibilityGuard groupVisibilityGuard,
        ILogger<GetScenarioSummaryQueryHandler> logger)
        : base(logger)
    {
        _repository = repository;
        _summaryBuilder = summaryBuilder;
        _groupVisibilityGuard = groupVisibilityGuard;
    }

    public async Task<ScenarioSummaryDto?> Handle(GetScenarioSummaryQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var scenario = await _repository.Get(request.Id);
            if (scenario is null || !await _groupVisibilityGuard.IsScenarioVisibleAsync(scenario, cancellationToken))
            {
                return null;
            }

            return await _summaryBuilder.BuildAsync(scenario.Token, cancellationToken);
        }, nameof(Handle), new { request.Id });
    }
}
