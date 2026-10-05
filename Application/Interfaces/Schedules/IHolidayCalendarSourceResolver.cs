// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Explains which holiday calendar decides holiday-work warnings and holiday surcharges for a contract calendar
/// selection, in the order IClientHolidayCalendarResolver applies it. It only names the source; the holidays
/// themselves always come from IClientHolidayCalendarResolver.
/// </summary>

using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.Api.Application.Interfaces.Schedules;

public interface IHolidayCalendarSourceResolver
{
    /// <summary>
    /// Resolves the calendar source and its display name.
    /// </summary>
    /// <param name="contractCalendarSelectionId">The effective contract's calendar selection, null when it has none</param>
    /// <param name="cancellationToken">Cancels the settings and selection reads</param>
    Task<ResolvedHolidayCalendarSource> ResolveAsync(Guid? contractCalendarSelectionId, CancellationToken cancellationToken = default);
}
