// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Schedules;

/// <summary>
/// A proposed placement that propose_plan did NOT write, with the reason (e.g. shift not found, or
/// it would introduce a collision). Surfaced so the supervised proposal fails loudly per placement
/// instead of silently dropping work.
/// </summary>
/// <param name="ClientId">Employee that was to be assigned</param>
/// <param name="ShiftId">Shift that was to be filled</param>
/// <param name="Date">Workday</param>
/// <param name="Reason">Why the placement was skipped</param>
public sealed record RejectedPlacement(Guid ClientId, Guid ShiftId, DateOnly Date, string Reason);
