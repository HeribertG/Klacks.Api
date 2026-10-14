// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Extracts the curated skill effect of every function offered in a replay's toolset, so the scorer can tell a
/// write from a read without access to the skill cache. Functions without a curated effect are left out.
/// </summary>
/// <param name="functions">The functions of the assembled toolset</param>

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Evaluation.TurnEval;

public static class TurnReplayToolEffects
{
    public static Dictionary<string, SkillEffect> From(IEnumerable<LLMFunction> functions)
    {
        var effects = new Dictionary<string, SkillEffect>(StringComparer.OrdinalIgnoreCase);
        foreach (var function in functions)
        {
            if (function.Effect is { } effect)
            {
                effects[function.Name] = effect;
            }
        }

        return effects;
    }
}
