// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.Services.Schedules;

/// <summary>
/// A single shift occurrence to evaluate for eligibility: the shift definition on a concrete date.
/// </summary>
/// <param name="ShiftId">The shift definition</param>
/// <param name="Date">The calendar day the slot falls on</param>
public readonly record struct EligibilitySlot(Guid ShiftId, DateOnly Date);
