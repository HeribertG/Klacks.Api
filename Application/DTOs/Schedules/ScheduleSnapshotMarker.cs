// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Schedules;

/// <summary>
/// Fingerprint of the movable part of a schedule at the moment a wizard run started. Comparing it again
/// at apply time tells whether somebody changed the plan underneath the run, which would otherwise cost
/// those manual changes silently.
/// UpdateTime is deliberately not part of the hash: surcharge and overtime recomputes touch work rows
/// without moving anything, and would raise false conflicts.
/// </summary>
/// <param name="From">First day of the period the run covered.</param>
/// <param name="Until">Last day of the period the run covered.</param>
/// <param name="AgentIds">Agents the run planned for.</param>
/// <param name="AnalyseToken">Scenario token the run was based on, null for the real plan.</param>
/// <param name="MovableWorkCount">Number of unlocked works in scope.</param>
/// <param name="StandaloneBreakCount">Number of breaks in scope that hang off no work.</param>
/// <param name="PlacementHash">Hash over the placement-relevant fields of those rows.</param>
public sealed record ScheduleSnapshotMarker(
    DateOnly From,
    DateOnly Until,
    IReadOnlyList<Guid> AgentIds,
    Guid? AnalyseToken,
    int MovableWorkCount,
    int StandaloneBreakCount,
    string PlacementHash);
