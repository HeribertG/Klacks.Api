// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Infrastructure.Services.Schedules;

/// <param name="GroupId">The group currently being viewed in the Original (real) schedule</param>
/// <param name="PeriodFrom">Start of the viewed date range</param>
/// <param name="PeriodUntil">End of the viewed date range</param>
public sealed record Wizard4TriggerTarget(Guid GroupId, DateOnly PeriodFrom, DateOnly PeriodUntil);
