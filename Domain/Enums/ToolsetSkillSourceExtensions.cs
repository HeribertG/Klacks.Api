// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Strength of a deterministic skill guarantee, shared by the resolver (which source a skill keeps when several
/// layers guarantee it) and the assembler (which guaranteed skills survive the provider cap): RecipeStep over
/// LearnedPhrase over Keyword over Hint over everything else, with the follow-through guarantee last.
/// BroadKeyword sits below even that, so the first real claim on the same skill replaces it, and it is the one
/// guarantee that does not lift a skill out of the retrieval tier.
/// </summary>

namespace Klacks.Api.Domain.Enums;

public static class ToolsetSkillSourceExtensions
{
    private const int RecipeStepPriority = 5;
    private const int LearnedPhrasePriority = 4;
    private const int KeywordPriority = 3;
    private const int HintPriority = 2;
    private const int DefaultPriority = 1;
    private const int BroadKeywordPriority = 0;

    public static int Priority(this ToolsetSkillSource source) => source switch
    {
        ToolsetSkillSource.RecipeStep => RecipeStepPriority,
        ToolsetSkillSource.LearnedPhrase => LearnedPhrasePriority,
        ToolsetSkillSource.Keyword => KeywordPriority,
        ToolsetSkillSource.Hint => HintPriority,
        ToolsetSkillSource.BroadKeyword => BroadKeywordPriority,
        _ => DefaultPriority
    };

    /// <summary>
    /// True for every guarantee that holds its skill above the retrieved skills at truncation - all of them
    /// except BroadKeyword.
    /// </summary>
    public static bool HoldsAboveRetrieval(this ToolsetSkillSource source) =>
        source != ToolsetSkillSource.BroadKeyword;
}
