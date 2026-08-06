// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Schedules;

/// <summary>
/// Cheap identity of the real plan for one selection. The background optimiser spends minutes on a
/// candidate; if the plan moved in the meantime the candidate describes a schedule that no longer
/// exists. Counts catch inserts and deletes, the newest timestamp catches an edit that keeps the count.
/// </summary>
/// <param name="WorkCount">Works of the selection in the period.</param>
/// <param name="BreakCount">Breaks of the selection in the period.</param>
/// <param name="MaxTimestamp">Newest create or update time across both; null when nothing exists.</param>
public sealed record Wizard4PlanFingerprint(int WorkCount, int BreakCount, DateTime? MaxTimestamp);
