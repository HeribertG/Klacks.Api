// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Decides which holiday calendar a holiday question is about. Precedence: a named calendar selection, then a
/// country/region, then the company calendar.
/// </summary>

using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.Api.Application.Interfaces.Schedules;

public interface IHolidayCalendarTargetResolver
{
    /// <summary>
    /// Resolves the calendar; returns an error with the real calendar names when a named selection is unknown or
    /// ambiguous.
    /// </summary>
    /// <param name="calendarSelectionName">Name of a configured calendar selection, optional</param>
    /// <param name="country">ISO country code, optional</param>
    /// <param name="state">Region code, optional</param>
    /// <param name="cancellationToken">Cancels the reads</param>
    Task<(HolidayCalendarTarget? Target, string? Error)> ResolveAsync(
        string? calendarSelectionName,
        string? country,
        string? state,
        CancellationToken cancellationToken = default);
}
