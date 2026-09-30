// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Grouping;

/// <summary>
/// A shift no city group could be derived for; its current group links stay untouched.
/// </summary>
/// <param name="ShiftId">Id of the shift.</param>
/// <param name="ShiftName">Name of the shift.</param>
/// <param name="CustomerName">Display name of its customer; empty when the shift has none.</param>
/// <param name="Reason">Why no city group could be derived.</param>
public sealed record UnassignableShift(Guid ShiftId, string ShiftName, string CustomerName, string Reason);
