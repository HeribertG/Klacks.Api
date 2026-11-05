// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Schedules.KeywordWishes;

namespace Klacks.Api.Application.Interfaces.Schedules;

/// <summary>
/// Counts the schedule-command wishes a persisted plan misses (broken FREE / OnlyX / NoX, unfulfilled -FREE / OnlyX),
/// through the same calculator the wizards optimise with.
/// </summary>
public interface IKeywordWishCountService
{
    /// <summary>Missed wishes of the given employees in the date range of one plan.</summary>
    /// <param name="clientIds">Employees to count.</param>
    /// <param name="from">First day (inclusive).</param>
    /// <param name="until">Last day (inclusive).</param>
    /// <param name="analyseToken">Scenario token of the plan; null = the real plan.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<KeywordWishesDto> CountAsync(
        IReadOnlyCollection<Guid> clientIds,
        DateOnly from,
        DateOnly until,
        Guid? analyseToken,
        CancellationToken cancellationToken);
}
