// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Services.Shifts;

/// <summary>
/// Snapshot of how much sporadic capacity has already been consumed at a candidate booking moment.
/// </summary>
/// <param name="EngagedAtDay">Existing bookings on the candidate day for this shift.</param>
/// <param name="DistinctBookedDays">Number of distinct workdays in the range that already carry at least one booking.</param>
public record SporadicCapacityUsage(int EngagedAtDay, int DistinctBookedDays);
