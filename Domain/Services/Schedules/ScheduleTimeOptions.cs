// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Options that control how schedule time intervals are materialized into ScheduleBlock instances.
/// </summary>
namespace Klacks.Api.Domain.Services.Schedules;

public class ScheduleTimeOptions
{
    public const string SectionName = "Schedule";

    /// <summary>
    /// When true, ScheduleBlock Start/End values are converted from local wall-clock time to UTC
    /// using <see cref="TimeZoneId"/>. Invalid times caused by spring-forward DST transitions are
    /// snapped forward to the next valid instant; ambiguous times caused by fall-back DST
    /// transitions use the standard offset (post-DST). When false (default) the legacy
    /// wall-clock semantics are kept (Kind = Unspecified, no DST handling).
    /// </summary>
    public bool DstAware { get; set; }

    /// <summary>
    /// IANA or Windows time zone id used when <see cref="DstAware"/> is true.
    /// </summary>
    public string TimeZoneId { get; set; } = "Europe/Zurich";
}
