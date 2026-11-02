// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Handler for retrieving a single AnalyseScenario by ID; a scenario outside the caller's group visibility is
/// answered like a missing one (null).
/// </summary>
/// <param name="Id">ID of the requested scenario</param>
/// <param name="variantRepository">Reads the variants (keys) the scenario carries</param>
/// <param name="groupVisibilityGuard">Decides whether the caller may see the scenario's group</param>

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.AnalyseScenarios;
using Klacks.Api.Application.Services.Groups;
using Klacks.Api.Application.Services.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.AnalyseScenarios;

public class GetAnalyseScenarioQueryHandler : BaseHandler, IRequestHandler<GetAnalyseScenarioQuery, AnalyseScenarioResource?>
{
    private readonly IAnalyseScenarioRepository _repository;
    private readonly IScenarioVariantRepository _variantRepository;
    private readonly IGroupVisibilityGuard _groupVisibilityGuard;

    public GetAnalyseScenarioQueryHandler(
        IAnalyseScenarioRepository repository,
        IScenarioVariantRepository variantRepository,
        IGroupVisibilityGuard groupVisibilityGuard,
        ILogger<GetAnalyseScenarioQueryHandler> logger)
        : base(logger)
    {
        _repository = repository;
        _variantRepository = variantRepository;
        _groupVisibilityGuard = groupVisibilityGuard;
    }

    public async Task<AnalyseScenarioResource?> Handle(GetAnalyseScenarioQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var scenario = await _repository.Get(request.Id);

            if (scenario == null || !await _groupVisibilityGuard.IsScenarioRowVisibleAsync(scenario, cancellationToken))
            {
                return null;
            }

            var variantIds = await _variantRepository.GetVariantIdsAsync(scenario.Token, cancellationToken);

            return AnalyseScenarioResourceFactory.ToResource(scenario, variantIds);
        }, nameof(Handle), new { request.Id });
    }
}
