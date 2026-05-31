// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Schedules;

/// <summary>
/// A slot of the absent employee that cover_absence could NOT auto-cover, with the reason
/// (no eligible candidate -&gt; under-coverage, or the absent work is locked and needs manual review).
/// </summary>
/// <param name="ShiftId">The shift that remains uncovered (original shift id)</param>
/// <param name="Date">Workday</param>
/// <param name="Reason">Why it could not be covered</param>
public sealed record UncoveredSlot(Guid ShiftId, DateOnly Date, string Reason);
