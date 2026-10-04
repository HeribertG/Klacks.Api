// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.Services.Schedules;

/// <summary>
/// Request payload that drives a harmonizer (Wizard 2) run. The harmonizer reads the
/// already-saved schedule for the listed agents and date range, builds an internal bitmap,
/// and improves harmony via a genetic algorithm. AnalyseToken isolates the source schedule
/// when running on a non-main scenario.
/// </summary>
/// <param name="PeriodFrom">Start of the date range the harmonizer reads (inclusive)</param>
/// <param name="PeriodUntil">End of the date range the harmonizer reads (inclusive)</param>
/// <param name="AgentIds">Clients whose rows are part of the bitmap</param>
/// <param name="AnalyseToken">Source-scenario isolation token; null reads the main scenario</param>
/// <param name="ContextDaysBefore">
/// Days before <paramref name="PeriodFrom"/> that are loaded as boundary context (works/breaks).
/// Boundary entries land in BitmapInput.BoundaryAssignments — the bitmap itself stays sized to the period —
/// so future boundary-aware validators can see runs that cross period edges. Default 14.
/// </param>
/// <param name="ContextDaysAfter">Same as <paramref name="ContextDaysBefore"/>, after <paramref name="PeriodUntil"/>.</param>
/// <param name="Seed">
/// Random seed of the run. Null draws a fresh one and records it, so a reported result can be replayed;
/// passing a recorded seed reproduces that run exactly.
/// </param>
/// <param name="LoadPlanningRules">
/// True (default) loads the approved planning rules (CounterRule + PlanningConstraint) into BitmapInput.Rules, so
/// Wizard 2/3 respect them. Wizard 4 passes false until its own rule hooks exist (constraint schema stage 6).
/// </param>
public sealed record HarmonizerContextRequest(
    DateOnly PeriodFrom,
    DateOnly PeriodUntil,
    IReadOnlyList<Guid> AgentIds,
    Guid? AnalyseToken,
    int ContextDaysBefore = 14,
    int ContextDaysAfter = 14,
    int? Seed = null,
    bool LoadPlanningRules = true);
