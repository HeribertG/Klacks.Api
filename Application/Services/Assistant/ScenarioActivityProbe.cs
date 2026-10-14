// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// State-based signal for the scenario follow-up guarantee: true while the user may see an AutoWizard run in
/// progress or a recently opened proposal scenario (created or updated within ScenarioPendingDetector.MinimumPendingHours,
/// so a forgotten old proposal does not occupy a toolset slot forever). Visibility is resolved for the turn's own
/// user through IGroupVisibilityService, the one fail-closed truth behind the REST/UI paths and the skills: an empty or
/// unknown user sees nothing, a restricted user only what lies in their visible groups, and a group-less run or scenario
/// spans all groups and therefore only counts for an unrestricted user (the rule of GetActiveAutofillRunsQueryHandler
/// and ListScenariosSkill). Cheapest first: with no run in progress, one unscoped existence query decides most turns
/// without resolving any scope; the scope and a scoped query are only needed once something exists.
/// </summary>
/// <param name="presenceTracker">The autofill runs in progress, unfiltered</param>
/// <param name="scenarioRepository">Answers whether a recent open scenario exists, optionally inside the visible roots</param>
/// <param name="groupVisibilityService">Resolves the visibility scope of the turn's user</param>
/// <param name="timeProvider">Clock for the recency window</param>

using Klacks.Api.Application.DTOs.Schedules.AutoWizard;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Assistant;
using Klacks.Api.Application.Interfaces.Schedules.AutoWizard;
using Klacks.Api.Application.Services.Assistant.Triggers;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Models.Associations;

namespace Klacks.Api.Application.Services.Assistant;

public class ScenarioActivityProbe : IScenarioActivityProbe
{
    private readonly IAutofillRunPresenceTracker _presenceTracker;
    private readonly IAnalyseScenarioRepository _scenarioRepository;
    private readonly IGroupVisibilityService _groupVisibilityService;
    private readonly TimeProvider _timeProvider;

    public ScenarioActivityProbe(
        IAutofillRunPresenceTracker presenceTracker,
        IAnalyseScenarioRepository scenarioRepository,
        IGroupVisibilityService groupVisibilityService,
        TimeProvider timeProvider)
    {
        _presenceTracker = presenceTracker;
        _scenarioRepository = scenarioRepository;
        _groupVisibilityService = groupVisibilityService;
        _timeProvider = timeProvider;
    }

    public async Task<bool> HasVisibleActivityAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            return false;
        }

        var runs = _presenceTracker.GetActive();
        var activeSinceUtc = _timeProvider.GetUtcNow().UtcDateTime.AddHours(-ScenarioPendingDetector.MinimumPendingHours);

        bool? anyRecentOpenScenario = null;
        if (runs.Count == 0)
        {
            anyRecentOpenScenario = await _scenarioRepository.AnyOpenVisibleAsync(null, activeSinceUtc, cancellationToken);
            if (anyRecentOpenScenario == false)
            {
                return false;
            }
        }

        var scope = await _groupVisibilityService.GetVisibilityScopeForUserAsync(
            userId.ToString(), cancellationToken);
        if (!scope.HasVisibleGroups)
        {
            return false;
        }

        if (HasVisibleRun(runs, scope))
        {
            return true;
        }

        if (scope.IsUnrestricted)
        {
            return anyRecentOpenScenario
                ?? await _scenarioRepository.AnyOpenVisibleAsync(null, activeSinceUtc, cancellationToken);
        }

        return await _scenarioRepository.AnyOpenVisibleAsync(scope.VisibleRootIds, activeSinceUtc, cancellationToken);
    }

    private static bool HasVisibleRun(IReadOnlyList<AutofillRunPresenceDto> runs, GroupVisibilityScope scope)
    {
        if (runs.Count == 0)
        {
            return false;
        }

        if (scope.IsUnrestricted)
        {
            return true;
        }

        var visibleGroupIds = scope.VisibleGroupIds.ToHashSet();
        return runs.Any(run => run.GroupId is { } groupId && visibleGroupIds.Contains(groupId));
    }
}
