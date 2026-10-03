// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Write model of a planning constraint (POST and PUT body). It deliberately carries no Id, Origin, approval
/// state or decision trail: the server assigns the id and sets every one of those itself.
/// </summary>

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.DTOs.Scheduling;

public class PlanningConstraintWriteResource
{
    public PlanningConstraintKind Kind { get; set; }

    public PlanningConstraintSeverity Severity { get; set; }

    public double Weight { get; set; }

    public PlanningConstraintScopeType ScopeType { get; set; }

    public Guid? ScopeId { get; set; }

    public string ParametersJson { get; set; } = string.Empty;

    public DateOnly? ValidFrom { get; set; }

    public DateOnly? ValidUntil { get; set; }

    public string? SourceText { get; set; }

    public string? Paraphrase { get; set; }

    public Guid? AnalyseToken { get; set; }
}
