// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Strength of a deterministic skill guarantee, shared by the resolver (which source a skill keeps when several
/// layers guarantee it) and the assembler (which guaranteed skills survive the provider cap): RecipeStep over
/// LearnedPhrase over Keyword over Hint over everything else, with the follow-through guarantee last.
/// </summary>

namespace Klacks.Api.Domain.Enums;

public static class ToolsetSkillSourceExtensions
{
    private const int RecipeStepPriority = 5;
    private const int LearnedPhrasePriority = 4;
    private const int KeywordPriority = 3;
    private const int HintPriority = 2;

    public static int Priority(this ToolsetSkillSource source) => source switch
    {
        ToolsetSkillSource.RecipeStep => RecipeStepPriority,
        ToolsetSkillSource.LearnedPhrase => LearnedPhrasePriority,
        ToolsetSkillSource.Keyword => KeywordPriority,
        ToolsetSkillSource.Hint => HintPriority,
        _ => 1
    };
}
