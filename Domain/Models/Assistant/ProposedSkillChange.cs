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

    public string Origin { get; set; } = ProposedChangeOrigins.Correction;

    public string ValueBefore { get; set; } = string.Empty;

    public string ValueAfter { get; set; } = string.Empty;

    public string Justification { get; set; } = string.Empty;

    public string Status { get; set; } = ProposedChangeStatuses.Pending;

    public string EvidenceJson { get; set; } = "[]";

    /// <summary>
    /// What the gate measured for this proposal, as JSON (GoldsetGateMetrics): reference run, model, scorer
    /// version, paired replay counts, fixed and regressed item ids, net gain and verdict. Null for a proposal
    /// no gate has measured.
    /// </summary>
    public string? GateMetricsJson { get; set; }

    public string? ReviewedBy { get; set; }

    public DateTime? ReviewedAt { get; set; }
}
