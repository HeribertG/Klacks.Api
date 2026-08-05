// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Per-day, per-agent, per-evaluator-version telemetry counters of the grounding shadow run.
/// Persisted (not logged) because production logging is Warning-level; "clean" and "no verdict"
/// are separate counters so a broken evaluator can never masquerade as an honest assistant.
/// </summary>

using Klacks.Api.Domain.Common;

namespace Klacks.Api.Domain.Models.Assistant.Grounding;

public class AnswerGroundingDailyCounter : BaseEntity
{
    public DateOnly Day { get; set; }

    public Guid AgentId { get; set; }

    public int EvaluatorVersion { get; set; }

    public int TurnsEvaluated { get; set; }

    public int TurnsClean { get; set; }

    public int TurnsWithFindings { get; set; }

    public int TurnsSkipped { get; set; }

    public int TurnsNoVerdict { get; set; }

    public int ClaimsExtracted { get; set; }

    public int ClaimsUncovered { get; set; }
}
