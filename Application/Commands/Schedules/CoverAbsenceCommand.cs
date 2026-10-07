// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Schedules;

/// <summary>
/// Reactive disruption flow (Gebiet J): an employee falls out on a day or a period, so the work is
/// covered as an isolated scenario. Everything lands in the scenario for atomic review: the absence
/// (Break) plus a Replacement WorkChange per slot pointing to a rule-compliant replacement. Locked
/// slots are reported for manual review; slots with no eligible candidate as under-coverage.
/// </summary>
/// <param name="ClientId">Employee who is absent</param>
/// <param name="Date">First (or only) day of the absence</param>
/// <param name="GroupId">Group / planning blade</param>
/// <param name="AbsenceId">Absence type (sick/vacation/...)</param>
/// <param name="UntilDate">Optional last day of the absence; null covers just Date</param>
/// <param name="OverrideBlock">
/// K1 supervisor override: when true and the caller holds the Admin or Authorised role, a recovery
/// delta blocked only by Block-mode compliance enforcement (never a structural error such as a
/// collision or a missing mandatory qualification) is materialised anyway and logged as an override.
/// </param>
/// <param name="Language">The planner's language for the scenario name prefix; null falls back to the installation language</param>
/// <param name="NotifyEscalationRoster">
/// Whether to start the planner call list (escalation chain) for every affected day. True for unattended
/// callers (skill / MCP / messenger report), where the chain's job is to find a human; false when a planner
/// triggers the flow interactively, because that planner has already taken charge and waking the roster
/// would only produce noise.
/// </param>
/// <param name="ReportedAtUtc">When the absence was reported (inbound receive time for the messenger); null means now</param>
/// <param name="Source">Path that started the run; stored on every replacement request the run records</param>
public record CoverAbsenceCommand(
    Guid ClientId,
    DateOnly Date,
    Guid GroupId,
    Guid AbsenceId,
    DateOnly? UntilDate = null,
    bool OverrideBlock = false,
    string? Language = null,
    bool NotifyEscalationRoster = true,
    DateTime? ReportedAtUtc = null,
    ReplacementRequestSource Source = ReplacementRequestSource.RecoveryEngine) : IRequest<CoverAbsenceOutcome>;
