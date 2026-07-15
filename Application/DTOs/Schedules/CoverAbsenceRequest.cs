// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Schedules;

/// <summary>
/// Request to cover a single-day absence: who is absent, on which day, in which group, with which absence type.
/// </summary>
/// <param name="ClientId">Employee who is absent</param>
/// <param name="Date">Day of the absence</param>
/// <param name="GroupId">Group / planning blade</param>
/// <param name="AbsenceId">Absence type (sick/vacation/...)</param>
/// <param name="OverrideBlock">K1 supervisor override for a Block-mode compliance escalation (never a structural error)</param>
public sealed record CoverAbsenceRequest(
    Guid ClientId,
    DateOnly Date,
    Guid GroupId,
    Guid AbsenceId,
    bool OverrideBlock = false);
