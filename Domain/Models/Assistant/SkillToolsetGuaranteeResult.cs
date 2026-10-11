// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Output of ISkillToolsetGuaranteeResolver.ResolveAsync.
/// </summary>
/// <param name="GuaranteedSkills">Skills guaranteed for this turn, independent of retrieval.</param>
/// <param name="GuaranteedSources">The strongest deterministic reason each guaranteed skill was added.</param>
/// <param name="LowestRankedSkillNames">Guaranteed skills that rank below every other guarantee of the same source at
/// truncation (the same-skill continuation), so they are the first guaranteed skills the provider cap drops.</param>
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Assistant;

public sealed record SkillToolsetGuaranteeResult(
    HashSet<AgentSkill> GuaranteedSkills,
    Dictionary<string, ToolsetSkillSource> GuaranteedSources,
    IReadOnlySet<string> LowestRankedSkillNames);
