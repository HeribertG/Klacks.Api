// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Output of ISkillToolsetGuaranteeResolver.ResolveAsync.
/// </summary>
/// <param name="GuaranteedSkills">Skills guaranteed for this turn, independent of retrieval.</param>
/// <param name="GuaranteedSources">The strongest deterministic reason each guaranteed skill was added.</param>
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Assistant;

public sealed record SkillToolsetGuaranteeResult(
    HashSet<AgentSkill> GuaranteedSkills,
    Dictionary<string, ToolsetSkillSource> GuaranteedSources);
