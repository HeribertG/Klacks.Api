// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// What ending a shift family on one day does to its rows.
/// </summary>
/// <param name="RowsToTrim">Rows staying in the family that get the new end date</param>
/// <param name="RowsToRemove">Derived rows starting after the day, each with its whole subtree</param>

namespace Klacks.Api.Domain.Models.Schedules;

public sealed record ShiftFamilyEndPlan(IReadOnlyList<Shift> RowsToTrim, IReadOnlyList<Shift> RowsToRemove);
