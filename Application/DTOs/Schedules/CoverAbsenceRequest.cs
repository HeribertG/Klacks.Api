// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

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
/// <param name="Language">The planner's language for the scenario name prefix; null falls back to the installation language.</param>
/// <param name="ReportedAtUtc">When the absence was reported (e.g. the time of the call); null means now. A future instant is clamped to now.</param>
/// <param name="NotifyEscalationRoster">Start the planner call list for the affected days; the UI sends false while the planner handles the absence interactively.</param>
public sealed record CoverAbsenceRequest(
    Guid ClientId,
    DateOnly Date,
    Guid GroupId,
    Guid AbsenceId,
    DateOnly? UntilDate = null,
    bool OverrideBlock = false,
    string? Language = null,
    bool NotifyEscalationRoster = true,
    DateTime? ReportedAtUtc = null);
