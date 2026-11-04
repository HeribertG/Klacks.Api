// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// What a reset does with the rows below a sealed origin row.
/// </summary>
/// <param name="RowsToClose">Kept rows that reach to the start date or beyond and end the day before it</param>
/// <param name="RowsToRemove">Rows that begin on or after the start date, soft-deleted</param>
/// <param name="HasKeptRowFromStart">True when a kept ancestor begins on or after the start date, so it cannot end before it</param>

namespace Klacks.Api.Domain.Models.Schedules;

public sealed record ShiftResetPlan(
    IReadOnlyList<Shift> RowsToClose,
    IReadOnlyList<Shift> RowsToRemove,
    bool HasKeptRowFromStart);
