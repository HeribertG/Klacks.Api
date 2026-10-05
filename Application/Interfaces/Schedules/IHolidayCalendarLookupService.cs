// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Computes the holidays of a country and region the way a calendar selection with exactly these entries would:
/// the national entry (country code twice, e.g. CH-CH) plus the regional one (e.g. CH-BE), rule status as stored,
/// no per-selection override. Used when a question names a region instead of a configured calendar.
/// </summary>

using Klacks.Api.Domain.Interfaces.CalendarSelections;

namespace Klacks.Api.Application.Interfaces.Schedules;

public interface IHolidayCalendarLookupService
{
    /// <summary>
    /// Loads the matching calendar rules once and returns a factory that builds the calculator for any year from
    /// them, so a multi-year question reads the rule table a single time.
    /// </summary>
    /// <param name="country">ISO country code, e.g. CH</param>
    /// <param name="state">Region code, e.g. BE; empty for the national calendar only</param>
    Task<Func<int, IHolidaysListCalculator>> PrepareAsync(string country, string? state);
}
