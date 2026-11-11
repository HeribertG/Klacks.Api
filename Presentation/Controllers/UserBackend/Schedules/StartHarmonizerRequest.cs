// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <param name="PeriodFrom">Start date (inclusive)</param>
/// <param name="PeriodUntil">End date (inclusive)</param>
/// <param name="AgentIds">Clients whose rows are part of the bitmap</param>
/// <param name="AnalyseToken">Source-scenario isolation token; null = main scenario</param>
/// <param name="KeywordMode">Hard (default) vetoes the restricting schedule commands; Soft treats them as wishes</param>
/// <param name="AgentOrderIsUserDefined">True when AgentIds reflects a roster order the planner arranged by hand; it is then
/// kept as the row order (stand-ins last). False (default) ranks automatically.</param>

using Klacks.ScheduleOptimizer.Models;

namespace Klacks.Api.Presentation.Controllers.UserBackend.Schedules;

public sealed record StartHarmonizerRequest(
    DateOnly PeriodFrom,
    DateOnly PeriodUntil,
    IReadOnlyList<Guid> AgentIds,
    Guid? AnalyseToken,
    KeywordMode KeywordMode = KeywordMode.Hard,
    bool AgentOrderIsUserDefined = false);
