// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Schedules;

/// <summary>
/// Selects rule-compliant replacement candidates for one slot (a shift on a date with concrete
/// start/end times, scoped to a group). Returns ranked eligible candidates + exclusions with reasons.
/// </summary>
/// <param name="ShiftId">Shift to fill</param>
/// <param name="Date">Workday</param>
/// <param name="StartTime">Slot start</param>
/// <param name="EndTime">Slot end</param>
/// <param name="GroupId">Group whose members are the candidate pool</param>
/// <param name="AnalyseToken">Optional scenario token; candidates are checked against the isolated scenario</param>
/// <param name="OverrideBlock">
/// K1 supervisor override: when true and the caller holds the Admin or Authorised role, a candidate
/// excluded only by a Block-mode compliance escalation (never a structural exclusion such as a
/// collision or a missing/insufficient mandatory qualification) stays eligible and is logged as an override.
/// </param>
public record FindReplacementQuery(
    Guid ShiftId,
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    Guid GroupId,
    Guid? AnalyseToken,
    bool OverrideBlock = false) : IRequest<ReplacementSearchResult>;
