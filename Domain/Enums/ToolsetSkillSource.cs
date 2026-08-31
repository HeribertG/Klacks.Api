// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Enums;

/// <summary>
/// Why a skill is in the per-turn toolset offered to the LLM (W1.6). Persisted per candidate in
/// <c>skill_selection_trajectories.knowledge_index_candidates_json</c> so the distribution of
/// chosen skills by provenance ("which source won") becomes a SQL query instead of a guess.
/// </summary>
public enum ToolsetSkillSource
{
    /// <summary>The skill is always-on and therefore in every toolset, independent of the turn.</summary>
    AlwaysOn = 1,

    /// <summary>The knowledge-index retrieval surfaced the skill above the cutoff.</summary>
    Retrieved = 2,

    /// <summary>A deterministic keyword/synonym match guaranteed the skill.</summary>
    Keyword = 3,

    /// <summary>A learned wording guaranteed the skill (the learned-phrase guarantee slot).</summary>
    LearnedPhrase = 4,

    /// <summary>A recipe forcing spine guaranteed the skill as a step of the active recipe.</summary>
    RecipeStep = 5,

    /// <summary>The co-required neighbour expansion pulled the skill into free budget.</summary>
    Expansion = 6,

    /// <summary>Any other deterministic hint (page explain, concept keyword, workflow pair,
    /// grouping intent, proposal confirmation, plan candidate, planning-profile loop, pending notes).</summary>
    Hint = 7
}
