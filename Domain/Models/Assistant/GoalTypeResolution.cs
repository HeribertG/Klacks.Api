// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The goal type a candidate stands for right now, derived from the installation's current state.
/// </summary>
/// <param name="IsVisible">False when the goal no longer fits the installation (setup done, or a goal that needs
/// plannable shifts while none exist); such a candidate is neither proposed, listed nor drafted.</param>
/// <param name="Definition">The catalogue entry to render and draft from; null for a candidate created before the
/// catalogue existed, which keeps its stored English text.</param>

namespace Klacks.Api.Domain.Models.Assistant;

public sealed record GoalTypeResolution(bool IsVisible, GoalTypeDefinition? Definition)
{
    public static readonly GoalTypeResolution Hidden = new(false, null);

    public static GoalTypeResolution Visible(GoalTypeDefinition? definition) => new(true, definition);
}
