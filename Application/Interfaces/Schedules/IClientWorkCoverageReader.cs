// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Lists the works that touch one calendar day of one person, read from the production timeline the holiday-work
/// warning uses: own top-level works, works covered as replacement, work changes applied, a night shift of the day
/// before that runs past midnight included.
/// </summary>

using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.Api.Application.Interfaces.Schedules;

public interface IClientWorkCoverageReader
{
    /// <summary>
    /// Returns the works touching the date in the real schedule (no scenario rows).
    /// </summary>
    /// <param name="clientId">The person</param>
    /// <param name="date">The calendar day in question</param>
    /// <param name="cancellationToken">Cancels the reads</param>
    Task<IReadOnlyList<HolidayOutcomeWork>> GetWorksTouchingAsync(Guid clientId, DateOnly date, CancellationToken cancellationToken = default);
}
