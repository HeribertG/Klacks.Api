// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.Services.Schedules;

/// <summary>
/// Validated input of a bundling: what bundle_nearby_timerange_shifts_into_container creates and what
/// evaluate_container_bundling previews.
/// </summary>
/// <param name="Location">City, place or address used as start and end base</param>
/// <param name="IsoWeekday">ISO weekday 1=Monday..7=Sunday</param>
/// <param name="StorageWeekday">The same weekday as stored (0=Sunday .. 6=Saturday)</param>
/// <param name="FromTime">Start of the day's time budget</param>
/// <param name="UntilTime">End of the day's time budget</param>
/// <param name="FromDate">First day of the container's validity</param>
/// <param name="UntilDate">Last day of the container's validity; null = open ended</param>
/// <param name="TransportModeText">Transport mode as the caller wrote it</param>
/// <param name="TransportMode">Parsed transport mode</param>
/// <param name="TimeRangeTolerance">Strictness of the task TimeRange windows</param>
/// <param name="GroupIdsRaw">Comma-separated group ids as given</param>
/// <param name="GroupName">Group name as given</param>
/// <param name="Name">Name of the container</param>
public sealed record ContainerBundleRequest(
    string Location,
    int IsoWeekday,
    int StorageWeekday,
    TimeOnly FromTime,
    TimeOnly UntilTime,
    DateOnly FromDate,
    DateOnly? UntilDate,
    string TransportModeText,
    ContainerTransportMode TransportMode,
    double TimeRangeTolerance,
    string? GroupIdsRaw,
    string? GroupName,
    string Name);
