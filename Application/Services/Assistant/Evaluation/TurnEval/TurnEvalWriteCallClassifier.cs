// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Decides whether a replayed tool call is a write, fail-closed in the same order as the production
/// Domain.Services.Assistant.ReadOnlyRecipeWriteGuard (internal, same assembly): navigation and catalogued read-only actions are no write; otherwise the curated
/// skill effect the replay recorded from the offered toolset decides (only Mutate is a write); a call
/// without a recorded effect - a tool outside the offered toolset, which is also what a hallucinated or
/// rights-forbidden skill looks like - falls back to the read-only name prefixes, and a name without one
/// counts as a write. Only the first tool call of each replay step is visible to the replay.
/// </summary>
/// <param name="replay">The replay whose steps are inspected; its ToolEffects supply the curated effects</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.Services.Assistant.Evaluation.TurnEval;

public static class TurnEvalWriteCallClassifier
{
    public static bool ReplayWroteAnything(TurnReplayResult replay)
    {
        if (replay.Steps.Count == 0)
        {
            return replay.ChosenTool != null && IsWriteCall(replay, replay.ChosenTool, replay.ToolParameters);
        }

        return replay.Steps.Any(step => step.Tool != null && IsWriteCall(replay, step.Tool, step.Parameters));
    }

    public static bool ReplayCalledAnyTool(TurnReplayResult replay) =>
        replay.ChosenTool != null || replay.Steps.Any(step => step.Tool != null);

    internal static bool IsWriteCall(TurnReplayResult replay, string tool, Dictionary<string, object> parameters)
    {
        if (string.Equals(tool, SkillNames.NavigateTo, StringComparison.OrdinalIgnoreCase)
            || ReadOnlySkillActions.IsReadOnlyCall(tool, parameters))
        {
            return false;
        }

        if (replay.ToolEffects.TryGetValue(tool, out var effect))
        {
            return effect == SkillEffect.Mutate;
        }

        return !ReadOnlySkillPrefixes.HasReadOnlyPrefix(tool);
    }
}
