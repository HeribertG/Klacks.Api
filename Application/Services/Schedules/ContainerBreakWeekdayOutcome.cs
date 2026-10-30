// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// What add_container_template_break did for one weekday template when it ran for all weekdays.
/// </summary>
/// <param name="Weekday">ISO weekday 1=Monday..7=Sunday</param>
/// <param name="Added">True when the break was added and confirmed by the re-read</param>
/// <param name="SkippedReason">Why the template was left unchanged; null when the break was added</param>
/// <param name="RouteReoptimized">True when the route of the template was re-optimized and its times re-read</param>
/// <param name="RouteError">Why the re-optimization did not happen; null when it succeeded or was not requested</param>

namespace Klacks.Api.Application.Services.Schedules;

public sealed record ContainerBreakWeekdayOutcome(
    int Weekday,
    bool Added,
    string? SkippedReason,
    bool RouteReoptimized,
    string? RouteError);
