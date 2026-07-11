// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Result of analysing the absence overlap of a group's active members over a date range.
/// </summary>
/// <param name="GroupId">Id of the resolved group.</param>
/// <param name="GroupName">Resolved name of the group.</param>
/// <param name="FromDate">First day of the analysed period.</param>
/// <param name="UntilDate">Last day of the analysed period.</param>
/// <param name="GroupSize">Number of active (non-customer) members of the group during the period.</param>
/// <param name="PeakCount">Highest number of members absent (booked or planned) on any single day.</param>
/// <param name="PeakDates">Day(s) on which PeakCount was reached.</param>
/// <param name="Days">Per-day breakdown of booked and planned absences.</param>

namespace Klacks.Api.Application.DTOs.Groups;

public record GetGroupAbsenceOverlapResult(
    Guid GroupId,
    string GroupName,
    DateOnly FromDate,
    DateOnly UntilDate,
    int GroupSize,
    int PeakCount,
    IReadOnlyList<DateOnly> PeakDates,
    IReadOnlyList<GroupAbsenceOverlapDay> Days);
