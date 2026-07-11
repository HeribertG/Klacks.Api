// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Absence overlap for a single day within a group absence overlap analysis.
/// </summary>
/// <param name="Date">The calendar day this entry describes.</param>
/// <param name="BookedCount">Number of members with a booked (scheduled) absence on this day.</param>
/// <param name="BookedMembers">The members with a booked absence on this day.</param>
/// <param name="PlannedCount">Number of members with a planned (placeholder) absence on this day.</param>
/// <param name="PlannedMembers">The members with a planned absence on this day.</param>
/// <param name="TotalAbsentCount">Distinct members absent on this day (booked and planned combined).</param>
/// <param name="PercentAbsent">TotalAbsentCount as a percentage of the group's size, rounded to one decimal.</param>

namespace Klacks.Api.Application.DTOs.Groups;

public record GroupAbsenceOverlapDay(
    DateOnly Date,
    int BookedCount,
    IReadOnlyList<GroupAbsenceOverlapMember> BookedMembers,
    int PlannedCount,
    IReadOnlyList<GroupAbsenceOverlapMember> PlannedMembers,
    int TotalAbsentCount,
    double PercentAbsent);
