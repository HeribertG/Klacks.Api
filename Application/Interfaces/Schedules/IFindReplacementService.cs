// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.Api.Application.Interfaces.Schedules;

/// <summary>
/// Selects rule-compliant replacement candidates for a single slot (a shift on a date with concrete
/// start/end times, scoped to a group). Hard-excludes collision / rest-time / blacklist / absence;
/// aggregate findings (overtime/consecutive/min-rest) are a soft ranking signal. Shared by the
/// find_replacement skill and the cover_absence flow. Does NOT yet check qualifications (v2, P4).
/// </summary>
public interface IFindReplacementService
{
    Task<ReplacementSearchResult> FindAsync(
        Guid shiftId,
        DateOnly date,
        TimeOnly startTime,
        TimeOnly endTime,
        Guid groupId,
        Guid? analyseToken,
        CancellationToken cancellationToken = default);
}
