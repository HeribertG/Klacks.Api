// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Strength of a deterministic skill guarantee, shared by the resolver (which source a skill keeps when several
/// layers guarantee it) and the assembler (which guaranteed skills survive the provider cap): RecipeStep over
/// LearnedPhrase over Keyword over Hint over everything else, with the follow-through guarantee last.
/// </summary>

namespace Klacks.Api.Domain.Enums;

public static class ToolsetSkillSourceExtensions
{
    public static int Priority(this ToolsetSkillSource source) => source switch
    {
        ToolsetSkillSource.RecipeStep => 5,
        ToolsetSkillSource.LearnedPhrase => 4,
        ToolsetSkillSource.Keyword => 3,
        ToolsetSkillSource.Hint => 2,
        _ => 1
    };
}
