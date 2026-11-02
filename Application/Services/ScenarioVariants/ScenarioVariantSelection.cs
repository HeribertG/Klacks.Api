// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Checks the variants (keys) selected for a new scenario. Keys need a scenario with a group (400). A missing or hidden key
/// is answered like a missing scenario (404), and that comes first so a hidden key does not reveal its status. A key that is
/// no longer Active is a conflict with the code of a consumed key (409), a key outside the scenario group and its subgroups is refused (400). An expired key
/// is allowed; the period only warns in the Ui. Selecting needs no right of its own, a scenario does not change the real plan.
/// </summary>
/// <param name="repository">Loads the selected variants</param>
/// <param name="scenarioService">Resolves the scenario group with its subgroups</param>
/// <param name="guard">Decides whether the caller may see a variant's group</param>
/// <param name="scenarioGroupId">Group of the new scenario; null = group-less</param>
/// <param name="variantIds">The selected variant ids; duplicates count once, null or empty selects nothing</param>

using Klacks.Api.Application.Constants;
using Klacks.Api.Application.Exceptions;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services.Groups;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.Services.ScenarioVariants;

public static class ScenarioVariantSelection
{
    public const string GrouplessScenarioMessage = "A scenario without a group cannot carry variants";

    public const string VariantOutsideGroupMessage = "A variant must belong to the scenario's group or one of its subgroups";

    public const string ConsumedMessage = "The variant has already been consumed by an accepted scenario and can no longer be selected";

    public static async Task<List<AnalyseScenario>> LoadSelectedAsync(
        IScenarioVariantRepository repository,
        IAnalyseScenarioService scenarioService,
        IGroupVisibilityGuard guard,
        Guid? scenarioGroupId,
        IReadOnlyList<Guid>? variantIds,
        CancellationToken cancellationToken)
    {
        var wanted = variantIds?.Distinct().ToList() ?? [];
        if (wanted.Count == 0)
        {
            return [];
        }

        if (scenarioGroupId is not { } groupId)
        {
            throw new InvalidRequestException(GrouplessScenarioMessage);
        }

        var variants = await repository.GetVariantsByIdsAsync(wanted, cancellationToken);
        var allowedGroupIds = await scenarioService.GetGroupHierarchyIdsAsync(groupId, cancellationToken);

        foreach (var id in wanted)
        {
            var variant = variants.Find(candidate => candidate.Id == id);
            if (!await guard.IsScenarioVisibleAsync(variant, cancellationToken))
            {
                throw new KeyNotFoundException(string.Format(ScenarioGroupVisibility.ScenarioNotFoundFormat, id));
            }

            if (variant!.Status != AnalyseScenarioStatus.Active)
            {
                throw new ScenarioVariantConflictException(ConsumedMessage, ScenarioVariantErrorCodes.Consumed);
            }

            if (variant.GroupId is not { } variantGroupId || !allowedGroupIds.Contains(variantGroupId))
            {
                throw new InvalidRequestException(VariantOutsideGroupMessage);
            }
        }

        return variants;
    }
}
