// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Groups;

/// <summary>
/// Result of assigning ungrouped employees to the group whose name exactly matches their address city.
/// With Applied=false it is a dry-run preview; with Applied=true the memberships were persisted and
/// re-read for verification.
/// </summary>
/// <param name="Applied">False for a dry-run preview, true when the memberships were persisted.</param>
/// <param name="TotalUngrouped">Total number of employees without any active (non-scenario) group membership.</param>
/// <param name="MatchCount">Number of ungrouped employees whose address city maps to exactly one group.</param>
/// <param name="AddedCount">Number of new memberships created (only meaningful when Applied is true).</param>
/// <param name="VerifiedCount">Number of created memberships re-read and confirmed in the database.</param>
/// <param name="NoAddressCount">Ungrouped employees skipped because they have no employee address / empty city.</param>
/// <param name="Assignments">The planned or applied city-to-group assignments.</param>
/// <param name="UnmatchedCities">Address cities with no equally named group, with the affected employee count.</param>
public record GroupUngroupedByCityNameResult(
    bool Applied,
    int TotalUngrouped,
    int MatchCount,
    int AddedCount,
    int VerifiedCount,
    int NoAddressCount,
    IReadOnlyList<GroupCityAssignment> Assignments,
    IReadOnlyList<UnmatchedCityCount> UnmatchedCities);
