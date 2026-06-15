// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Assistant;

public class SkillRelation : BaseEntity
{
    public Guid AgentId { get; set; }

    public string SkillAName { get; set; } = string.Empty;

    public string SkillBName { get; set; } = string.Empty;

    public SkillRelationType Type { get; set; }

    public double Confidence { get; set; }

    public int SupportCount { get; set; }

    public int ContradictionCount { get; set; }

    public string Provenance { get; set; } = string.Empty;

    public SkillRelationSource Source { get; set; }

    public DateTime? LastReinforcedAt { get; set; }

    public SkillRelationStatus Status { get; set; }

    public virtual Agent Agent { get; set; } = null!;
}
