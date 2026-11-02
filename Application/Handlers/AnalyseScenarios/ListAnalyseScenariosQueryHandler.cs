// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Handler for retrieving all AnalyseScenarios of a group. A group outside the caller's visibility - or the
/// group-less scenarios for a restricted caller - yields an empty list, exactly like a group without scenarios.
/// </summary>
/// <param name="GroupId">ID of the group whose scenarios are loaded; null loads the group-less scenarios</param>
/// <param name="variantRepository">Reads the variants (keys) of all listed scenarios in one query</param>
/// <param name="groupVisibilityGuard">Decides whether the caller may see the requested group</param>

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.AnalyseScenarios;
using Klacks.Api.Application.Services.Groups;
using Klacks.Api.Application.Services.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.AnalyseScenarios;

public class ListAnalyseScenariosQueryHandler : BaseHandler, IRequestHandler<ListAnalyseScenariosQuery, List<AnalyseScenarioResource>>
{
    private readonly IAnalyseScenarioRepository _repository;
    private readonly IScenarioVariantRepository _variantRepository;
    private readonly IGroupVisibilityGuard _groupVisibilityGuard;

    public ListAnalyseScenariosQueryHandler(
        IAnalyseScenarioRepository repository,
        IScenarioVariantRepository variantRepository,
        IGroupVisibilityGuard groupVisibilityGuard,
        ILogger<ListAnalyseScenariosQueryHandler> logger)
        : base(logger)
    {
        _repository = repository;
        _variantRepository = variantRepository;
        _groupVisibilityGuard = groupVisibilityGuard;
    }

    public async Task<List<AnalyseScenarioResource>> Handle(ListAnalyseScenariosQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            if (!await _groupVisibilityGuard.IsScenarioScopeVisibleAsync(request.GroupId, cancellationToken))
            {
                return new List<AnalyseScenarioResource>();
            }

            var scenarios = await _repository.GetByGroupAsync(request.GroupId, cancellationToken);

            var variantIdsByToken = await _variantRepository.GetVariantIdsByScenarioTokensAsync(
                scenarios.Select(s => s.Token).ToList(), cancellationToken);

            return scenarios
                .Select(s => AnalyseScenarioResourceFactory.ToResource(s, variantIdsByToken.GetValueOrDefault(s.Token, [])))
                .ToList();
        }, nameof(Handle), new { request.GroupId });
    }
}
