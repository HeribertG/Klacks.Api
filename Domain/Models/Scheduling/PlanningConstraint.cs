// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// A planning constraint the optimizer and the plan checks evaluate besides the contract caps and the
/// CounterRule counters: sequence patterns (MaxConsecutiveOfKind, ForbiddenTransition, RestAfterKind) and
/// soft team fairness. The kind-specific values live in <see cref="ParametersJson"/> (jsonb, carrying a
/// schemaVersion) and are validated per kind by IPlanningConstraintValidator. Only
/// <see cref="RuleApprovalStatus.Approved"/> rows are ever evaluated; an approved row is immutable - a change
/// creates a new row that points back via <see cref="PreviousVersionId"/> while the old row becomes Revoked.
/// Rows written by the region-setup import carry non-empty import keys (see <see cref="IImportableEntity"/>).
/// </summary>
/// <param name="Kind">Constraint family, selects the ParametersJson schema</param>
/// <param name="Severity">Hard vetoes a plan, Soft adds Weight times the excess to the penalty</param>
/// <param name="Weight">Penalty weight of a soft constraint</param>
/// <param name="ScopeType">Scope axis; ScopeId is null for Global and required otherwise</param>
/// <param name="ValidFrom">First calendar day the constraint applies (null = open start)</param>
/// <param name="ValidUntil">Last calendar day the constraint applies (null = open end)</param>
/// <param name="Origin">Who brought the constraint in (admin, LLM proposal, employee request, import)</param>
/// <param name="ApprovalStatus">Approval state; consumers only read Approved</param>
/// <param name="SourceText">Original wording the constraint was derived from (LLM intake), if any</param>
/// <param name="Paraphrase">Human-readable restatement shown in the pending list</param>
/// <param name="AnalyseToken">Scenario token; null for the real plan</param>

using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Scheduling;

public class PlanningConstraint : BaseEntity, IImportableEntity
{
    public PlanningConstraintKind Kind { get; set; }

    public PlanningConstraintSeverity Severity { get; set; }

    public double Weight { get; set; }

    public PlanningConstraintScopeType ScopeType { get; set; }

    public Guid? ScopeId { get; set; }

    public string ParametersJson { get; set; } = string.Empty;

    public DateOnly? ValidFrom { get; set; }

    public DateOnly? ValidUntil { get; set; }

    public RuleOrigin Origin { get; set; } = RuleOrigin.Admin;

    public RuleApprovalStatus ApprovalStatus { get; set; } = RuleApprovalStatus.Proposed;

    public string? SourceText { get; set; }

    public string? Paraphrase { get; set; }

    public string? ProposedBy { get; set; }

    public string? ApprovedBy { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public Guid? AnalyseToken { get; set; }

    public Guid? PreviousVersionId { get; set; }

    public string ImportSourceKey { get; set; } = string.Empty;

    public string ImportContentHash { get; set; } = string.Empty;
}
