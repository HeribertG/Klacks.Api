// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.Constants;
using Klacks.ScheduleOptimizer.Models;

namespace Klacks.Api.Application.Services.Schedules.HolisticHarmonizer;

/// <param name="PeriodFrom">Start of the date range to load (inclusive).</param>
/// <param name="PeriodUntil">End of the date range to load (inclusive).</param>
/// <param name="AgentIds">Clients whose rows are part of the bitmap.</param>
/// <param name="AnalyseToken">Source-scenario isolation token; null reads the main schedule.</param>
/// <param name="Language">UI language; null falls back to English.</param>
/// <param name="ContextDaysBefore">
/// Days before <paramref name="PeriodFrom"/> loaded as boundary context for future boundary-aware validators.
/// The bitmap stays sized to the period; boundary works/breaks land in BitmapInput.BoundaryAssignments. Default 14.
/// </param>
/// <param name="ContextDaysAfter">Same as <paramref name="ContextDaysBefore"/>, after <paramref name="PeriodUntil"/>.</param>
/// <param name="KeywordMode">Hard (default) vetoes the restricting schedule commands; Soft treats them as wishes.</param>
/// <param name="GroupId">Group the run harmonises for; null = no group. Closes the days outside the agents' group period
/// (see HarmonizerContextRequest.GroupId).</param>
/// <param name="AgentOrderIsUserDefined">True keeps the hand-arranged AgentIds order as the row order (see
/// HarmonizerContextRequest.AgentOrderIsUserDefined); false ranks automatically.</param>
public sealed record HolisticHarmonizerRunInput(
    DateOnly PeriodFrom,
    DateOnly PeriodUntil,
    IReadOnlyList<Guid> AgentIds,
    Guid? AnalyseToken,
    string? Language,
    int ContextDaysBefore = ScenarioConstants.BoundaryDays,
    int ContextDaysAfter = ScenarioConstants.BoundaryDays,
    KeywordMode KeywordMode = KeywordMode.Hard,
    Guid? GroupId = null,
    bool AgentOrderIsUserDefined = false);
