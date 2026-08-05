// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.Exceptions;

/// <summary>
/// A requested autofill run is larger than its family allows. Carries the measured and the permitted
/// figures so a controller can build the family-specific error body without recomputing anything.
/// </summary>
/// <param name="code">Machine-readable error code of the violated limit.</param>
/// <param name="family">Autofill family the run belongs to.</param>
/// <param name="agents">Requested agent count.</param>
/// <param name="shifts">Requested shift count.</param>
/// <param name="periodDays">Requested period length in days.</param>
/// <param name="maxAgents">Permitted agent count.</param>
/// <param name="maxShifts">Permitted shift count.</param>
/// <param name="maxPeriodDays">Permitted period length in days.</param>
/// <param name="slotProduct">Requested decision space; zero for families without that limit.</param>
/// <param name="maxSlotProduct">Permitted decision space; zero for families without that limit.</param>
public sealed class AutofillLimitExceededException : BadRequestException
{
    public AutofillLimitExceededException(
        string code,
        AutofillFamily family,
        int agents,
        int shifts,
        int periodDays,
        int maxAgents,
        int maxShifts,
        int maxPeriodDays,
        long slotProduct = 0,
        int maxSlotProduct = 0)
        : base($"The requested {family} run is too large: {agents} agents, {shifts} shifts, {periodDays} days.")
    {
        Code = code;
        Family = family;
        Agents = agents;
        Shifts = shifts;
        PeriodDays = periodDays;
        MaxAgents = maxAgents;
        MaxShifts = maxShifts;
        MaxPeriodDays = maxPeriodDays;
        SlotProduct = slotProduct;
        MaxSlotProduct = maxSlotProduct;
    }

    public string Code { get; }

    public AutofillFamily Family { get; }

    public int Agents { get; }

    public int Shifts { get; }

    public int PeriodDays { get; }

    public int MaxAgents { get; }

    public int MaxShifts { get; }

    public int MaxPeriodDays { get; }

    public long SlotProduct { get; }

    public int MaxSlotProduct { get; }
}
