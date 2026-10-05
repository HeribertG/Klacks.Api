// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Handler for the live summary of one AnalyseScenario; returns null when the scenario does not exist.
/// </summary>
/// <param name="repository">Resolves the scenario by its ID</param>
/// <param name="summaryBuilder">Computes the summary from the scenario's own shifts and works</param>

using Klacks.Api.Application.DTOs.Schedules.Summary;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Application.Queries.AnalyseScenarios;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.AnalyseScenarios;

public class GetScenarioSummaryQueryHandler : BaseHandler, IRequestHandler<GetScenarioSummaryQuery, ScenarioSummaryDto?>
{
    private readonly IAnalyseScenarioRepository _repository;
    private readonly IScenarioSummaryBuilder _summaryBuilder;

    public GetScenarioSummaryQueryHandler(
        IAnalyseScenarioRepository repository,
        IScenarioSummaryBuilder summaryBuilder,
        ILogger<GetScenarioSummaryQueryHandler> logger)
        : base(logger)
    {
        _repository = repository;
        _summaryBuilder = summaryBuilder;
    }

    public async Task<ScenarioSummaryDto?> Handle(GetScenarioSummaryQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var scenario = await _repository.Get(request.Id);
            if (scenario is null)
            {
                return null;
            }

            return await _summaryBuilder.BuildAsync(scenario.Token, cancellationToken);
        }, nameof(Handle), new { request.Id });
    }
}
