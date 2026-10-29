// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Output of ISkillToolsetGuaranteeResolver.ResolveAsync.
/// </summary>
/// <param name="GuaranteedSkills">Skills guaranteed for this turn, independent of retrieval.</param>
/// <param name="GuaranteedSources">The strongest deterministic reason each guaranteed skill was added.</param>
/// <param name="LowestRankedSkillNames">Guaranteed skills that rank below every other guarantee of the same source at
/// truncation (the same-skill continuation), so they are the first guaranteed skills the provider cap drops.</param>
/// <param name="TurnIntentVetoesForcedToolCall">The turn-intent classifier lifts the tool call the mutation detector would force.</param>
/// <param name="ExcludedSkillNames">Skills an open draft loop keeps out of this turn's toolset; null when none.</param>
/// <param name="DraftLoopOpen">A planning-profile setup or an order review is open in the conversation, so the recipe
/// guarantee matched by keyword trigger only.</param>

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Assistant;

public sealed record SkillToolsetGuaranteeResult(
    HashSet<AgentSkill> GuaranteedSkills,
    Dictionary<string, ToolsetSkillSource> GuaranteedSources,
    IReadOnlySet<string> LowestRankedSkillNames,
    bool TurnIntentVetoesForcedToolCall = false,
    IReadOnlySet<string>? ExcludedSkillNames = null,
    bool DraftLoopOpen = false);
