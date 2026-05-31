// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Schedules;

/// <summary>
/// Reactive disruption flow (Gebiet J): an employee falls out on a day, so the work is covered as an
/// isolated scenario. Everything lands in the scenario for atomic review: the absence (Break) plus a
/// Replacement WorkChange per slot pointing to a rule-compliant replacement. Locked slots are reported
/// for manual review; slots with no eligible candidate as under-coverage.
/// </summary>
/// <param name="ClientId">Employee who is absent</param>
/// <param name="Date">Day of the absence</param>
/// <param name="GroupId">Group / planning blade</param>
/// <param name="AbsenceId">Absence type (sick/vacation/...)</param>
public record CoverAbsenceCommand(
    Guid ClientId,
    DateOnly Date,
    Guid GroupId,
    Guid AbsenceId) : IRequest<CoverAbsenceOutcome>;
