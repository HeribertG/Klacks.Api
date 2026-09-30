// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Grouping;

/// <summary>
/// Outcome of assign_shifts_to_city_groups. With Applied=false nothing was written and every count
/// describes the plan.
/// </summary>
/// <param name="Applied">False for a preview, true when the links were persisted.</param>
/// <param name="TotalShifts">Number of shifts that matched the filter.</param>
/// <param name="SkippedAlreadyInCityGroupCount">Shifts skipped because they already hold a city-group link.</param>
/// <param name="AssignedCount">Shifts that were, or would be, moved into a city group.</param>
/// <param name="ReplacedLinkCount">Existing group links that were, or would be, removed by the move.</param>
/// <param name="VerifiedCount">New links re-read from the database after the write; 0 on a preview.</param>
/// <param name="UnassignableCount">Shifts no city group could be derived for.</param>
/// <param name="Targets">Per-city-group shift counts.</param>
/// <param name="AssignmentSample">A capped sample of the individual moves, with their match reason.</param>
/// <param name="UnassignableSample">A capped sample of the shifts that stayed where they are, with their reason.</param>
/// <param name="UnlocatedCityGroupNames">City groups without a known location, which a nearest match cannot pick.</param>
public sealed record AssignShiftsToCityGroupsResult(
    bool Applied,
    int TotalShifts,
    int SkippedAlreadyInCityGroupCount,
    int AssignedCount,
    int ReplacedLinkCount,
    int VerifiedCount,
    int UnassignableCount,
    IReadOnlyList<ShiftCityGroupTargetSummary> Targets,
    IReadOnlyList<ShiftCityGroupAssignment> AssignmentSample,
    IReadOnlyList<UnassignableShift> UnassignableSample,
    IReadOnlyList<string> UnlocatedCityGroupNames);
