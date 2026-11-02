// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Handler for listing the variants (keys) of a group and its subgroups, consumed ones included, ordered by name then id,
/// because a scenario may carry the keys of its group and of every subgroup. Only groups the caller may see count; a
/// requested group outside the caller's visibility yields an empty list, exactly like a group without variants.
/// </summary>
/// <param name="GroupId">Group whose variants, and those of its subgroups, are loaded</param>
/// <param name="variantRepository">Loads the variants of the groups and counts their services in one query</param>
/// <param name="groupVisibilityGuard">Decides whether the caller may see the requested group and each subgroup</param>
/// <param name="scenarioService">Resolves the group with its subgroups</param>
/// <param name="companyClock">Gives today in the company's time zone for the expiry flag</param>

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.ScenarioVariants;
using Klacks.Api.Application.Services.Groups;
using Klacks.Api.Application.Services.ScenarioVariants;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.ScenarioVariants;

public class ListScenarioVariantsQueryHandler : BaseHandler, IRequestHandler<ListScenarioVariantsQuery, List<ScenarioVariantResource>>
{
    private readonly IScenarioVariantRepository _variantRepository;
    private readonly IGroupVisibilityGuard _groupVisibilityGuard;
    private readonly IAnalyseScenarioService _scenarioService;
    private readonly ICompanyClock _companyClock;

    public ListScenarioVariantsQueryHandler(
        IScenarioVariantRepository variantRepository,
        IGroupVisibilityGuard groupVisibilityGuard,
        IAnalyseScenarioService scenarioService,
        ICompanyClock companyClock,
        ILogger<ListScenarioVariantsQueryHandler> logger)
        : base(logger)
    {
        _variantRepository = variantRepository;
        _groupVisibilityGuard = groupVisibilityGuard;
        _scenarioService = scenarioService;
        _companyClock = companyClock;
    }

    public async Task<List<ScenarioVariantResource>> Handle(ListScenarioVariantsQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            if (!await _groupVisibilityGuard.IsScenarioScopeVisibleAsync(request.GroupId, cancellationToken))
            {
                return new List<ScenarioVariantResource>();
            }

            var visibleGroupIds = new List<Guid>();
            foreach (var groupId in await _scenarioService.GetGroupHierarchyIdsAsync(request.GroupId, cancellationToken))
            {
                if (await _groupVisibilityGuard.IsGroupVisibleAsync(groupId, cancellationToken))
                {
                    visibleGroupIds.Add(groupId);
                }
            }

            var variants = await _variantRepository.ListByGroupsAsync(visibleGroupIds, cancellationToken);
            var counts = await _variantRepository.CountShiftsByVariantTokenAsync(
                variants.Select(v => v.Token).ToList(), cancellationToken);
            var today = await _companyClock.GetTodayDateAsync(cancellationToken);

            return variants
                .Select(v => ScenarioVariantResourceFactory.Create(v, counts.GetValueOrDefault(v.Token), today))
                .ToList();
        }, nameof(Handle), new { request.GroupId });
    }
}
