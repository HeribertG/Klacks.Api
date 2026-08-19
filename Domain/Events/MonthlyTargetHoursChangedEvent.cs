// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Events;

/// <summary>
/// Raised after a committed create, update or delete of a company-wide monthly target hours row.
/// The month value feeds guaranteed hours of inheriting and MonthlyTargetHours-interval contracts
/// and the absence macros, so persisted breaks and works of that month must be recalculated.
/// </summary>
/// <param name="Year">Calendar year of the affected month</param>
/// <param name="Month">Month number (1-12) of the affected month</param>
public sealed record MonthlyTargetHoursChangedEvent(int Year, int Month) : DomainEvent;
