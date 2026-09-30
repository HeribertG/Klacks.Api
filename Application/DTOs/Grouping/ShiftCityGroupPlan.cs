// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Grouping;

/// <summary>
/// Read-only result of ShiftCityGroupPlanner: which shift would move into which city group, which ones
/// already sit in a city group and which ones no city group could be derived for. Nothing here is persisted.
/// </summary>
/// <param name="TotalShifts">Number of shifts the planner was handed.</param>
/// <param name="SkippedAlreadyInCityGroupCount">Shifts left untouched because they already hold a link to a city group.</param>
/// <param name="Assignments">The planned moves.</param>
/// <param name="Unassignable">Shifts no city group could be derived for, with their reason.</param>
/// <param name="UnlocatedCityGroupNames">City groups with neither coordinates nor addresses carrying their name; a nearest match cannot pick them.</param>
public sealed record ShiftCityGroupPlan(
    int TotalShifts,
    int SkippedAlreadyInCityGroupCount,
    IReadOnlyList<ShiftCityGroupAssignment> Assignments,
    IReadOnlyList<UnassignableShift> Unassignable,
    IReadOnlyList<string> UnlocatedCityGroupNames);
