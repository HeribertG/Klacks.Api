// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Services.Shifts;

/// <summary>
/// Minimal shift projection required to validate a sporadic booking.
/// </summary>
/// <param name="Name">Display name used in conflict messages.</param>
/// <param name="IsSporadic">Whether the shift opts into sporadic-range blocking.</param>
/// <param name="SporadicScope">Scope that defines the blocking range.</param>
/// <param name="FromDate">Lower bound used when <see cref="SporadicScope"/> is <see cref="ShiftSporadic.ContractualTerm"/>.</param>
/// <param name="UntilDate">Upper bound used when <see cref="SporadicScope"/> is <see cref="ShiftSporadic.ContractualTerm"/>; null = open-ended.</param>
/// <param name="SumEmployees">Per-day capacity (number of parallel bookings allowed on a single day). Treated as 1 when zero.</param>
/// <param name="Quantity">Per-range capacity (number of distinct booked days allowed across the entire range). Treated as 1 when zero.</param>
public record SporadicShiftInfo(
    string Name,
    bool IsSporadic,
    ShiftSporadic SporadicScope,
    DateOnly FromDate,
    DateOnly? UntilDate,
    int SumEmployees,
    int Quantity)
{
    public int EffectiveSumEmployees => SumEmployees < 1 ? 1 : SumEmployees;
    public int EffectiveQuantity => Quantity < 1 ? 1 : Quantity;
}
