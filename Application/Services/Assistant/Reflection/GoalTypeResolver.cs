// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Resolves a candidate's goal type against the installation as it is now, the way WelcomeFocusResolver treats the
/// no_schedule_yet journal row: a stored candidate never decides its own stage, because its stage may have moved on
/// (and candidates stored before the stage existed carry none). "No schedule yet" picks the entry of the current
/// setup stage and disappears once work exists; a goal that requires plannable shifts disappears while there are
/// none, so the setup goal and the "seal open orders earlier" goal never show side by side. The setup state is read
/// at most once per instance (scoped: one request or one reflection cycle) and only for goal types that depend on it.
/// </summary>
/// <param name="setupProbe">Installation-wide snapshot along orders -> shifts -> assignments.</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Reflection;

public sealed class GoalTypeResolver : IGoalTypeResolver
{
    private readonly IScheduleActivityProbe _setupProbe;
    private ScheduleSetupState? _setupState;

    public GoalTypeResolver(IScheduleActivityProbe setupProbe)
    {
        _setupProbe = setupProbe;
    }

    public async Task<GoalTypeResolution> ResolveAsync(string? goalType, CancellationToken cancellationToken = default)
    {
        var definition = GoalTypeCatalog.Find(goalType);
        if (definition == null)
        {
            return GoalTypeResolution.Visible(null);
        }

        if (string.Equals(goalType, AgentTriggerKinds.NoScheduleYet, StringComparison.Ordinal))
        {
            var state = await GetSetupStateAsync(cancellationToken);
            return state.HasWork
                ? GoalTypeResolution.Hidden
                : GoalTypeResolution.Visible(GoalTypeCatalog.FindForStage(ScheduleSetupStages.For(state)));
        }

        if (definition.RequiresPlannableShifts && !(await GetSetupStateAsync(cancellationToken)).HasShifts)
        {
            return GoalTypeResolution.Hidden;
        }

        return GoalTypeResolution.Visible(definition);
    }

    private async Task<ScheduleSetupState> GetSetupStateAsync(CancellationToken cancellationToken)
    {
        _setupState ??= await _setupProbe.GetSetupStateAsync(cancellationToken);
        return _setupState;
    }
}
