// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Domain.Models.Assistant;

public class ProposedSkillChange : BaseEntity
{
    public Guid AgentId { get; set; }

    public Guid SkillId { get; set; }

    public string SkillName { get; set; } = string.Empty;

    public string Field { get; set; } = ProposedChangeFields.Description;

    public string ValueBefore { get; set; } = string.Empty;

    public string ValueAfter { get; set; } = string.Empty;

    public string Justification { get; set; } = string.Empty;

    public string Status { get; set; } = ProposedChangeStatuses.Pending;

    public string EvidenceJson { get; set; } = "[]";

    public string? ReviewedBy { get; set; }

    public DateTime? ReviewedAt { get; set; }
}
