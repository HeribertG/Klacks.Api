// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Read model of a planning constraint for the settings list and the pending list. Origin, approval state
/// and the decision trail are server-controlled and only travel outwards.
/// </summary>

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.DTOs.Scheduling;

public class PlanningConstraintResource
{
    public Guid Id { get; set; }

    public PlanningConstraintKind Kind { get; set; }

    public PlanningConstraintSeverity Severity { get; set; }

    public double Weight { get; set; }

    public PlanningConstraintScopeType ScopeType { get; set; }

    public Guid? ScopeId { get; set; }

    public string ParametersJson { get; set; } = string.Empty;

    public DateOnly? ValidFrom { get; set; }

    public DateOnly? ValidUntil { get; set; }

    public RuleOrigin Origin { get; set; }

    public RuleApprovalStatus ApprovalStatus { get; set; }

    public string? SourceText { get; set; }

    public string? Paraphrase { get; set; }

    public string? ProposedBy { get; set; }

    public string? ApprovedBy { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public Guid? AnalyseToken { get; set; }

    public Guid? PreviousVersionId { get; set; }

    public DateTime? CreateTime { get; set; }
}
