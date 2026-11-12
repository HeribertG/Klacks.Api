// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Schedules;

/// <summary>
/// The not-yet-committed hours of one client for the period-cap check of several clients in one call.
/// </summary>
/// <param name="ClientId">Employee the hours belong to</param>
/// <param name="PlannedHours">Not-yet-saved (date, hours) pairs; hours a plan takes away are negative</param>
public sealed record ClientPlannedHours(
    Guid ClientId,
    IReadOnlyList<(DateOnly Date, decimal Hours)> PlannedHours);
