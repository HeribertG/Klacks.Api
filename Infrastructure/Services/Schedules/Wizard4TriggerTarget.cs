// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Infrastructure.Services.Schedules;

/// <summary>
/// A group the planner is viewing in the Original schedule, with the viewed period; a candidate target for a Wizard 4 background run.
/// </summary>
/// <param name="GroupId">The group currently being viewed in the Original (real) schedule</param>
/// <param name="PeriodFrom">Start of the viewed date range</param>
/// <param name="PeriodUntil">End of the viewed date range</param>
public sealed record Wizard4TriggerTarget(Guid GroupId, DateOnly PeriodFrom, DateOnly PeriodUntil);
