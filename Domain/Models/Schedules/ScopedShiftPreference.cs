// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Schedules;

/// <summary>
/// A client's preference for one concrete shift, either set explicitly or inherited from an ancestor of that shift.
/// </summary>
/// <param name="ClientId">Client the preference belongs to</param>
/// <param name="ShiftId">Shift the preference applies to</param>
/// <param name="PreferenceType">Preferred or blacklisted</param>
public sealed record ScopedShiftPreference(
    Guid ClientId,
    Guid ShiftId,
    ShiftPreferenceType PreferenceType);