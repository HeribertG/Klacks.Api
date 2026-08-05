// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Schedules;

/// <summary>
/// Request to cover an absence: who is absent, over which period, in which group, with which absence type.
/// </summary>
/// <param name="ClientId">Employee who is absent</param>
/// <param name="Date">First day of the absence</param>
/// <param name="GroupId">Group / planning blade</param>
/// <param name="AbsenceId">Absence type (sick/vacation/...)</param>
/// <param name="UntilDate">Optional last day of the absence; null covers just Date</param>
/// <param name="OverrideBlock">K1 supervisor override for a Block-mode compliance escalation (never a structural error)</param>
public sealed record CoverAbsenceRequest(
    Guid ClientId,
    DateOnly Date,
    Guid GroupId,
    Guid AbsenceId,
    DateOnly? UntilDate = null,
    bool OverrideBlock = false);
