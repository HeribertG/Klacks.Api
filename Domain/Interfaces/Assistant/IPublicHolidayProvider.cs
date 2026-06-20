// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

/// <summary>
/// Resolves whether a public holiday falls today or tomorrow for a country, so the welcome greeting
/// can mention it. Always optional: returns null on no holiday or any upstream failure.
/// </summary>
public interface IPublicHolidayProvider
{
    /// <summary>
    /// Returns the holiday for today (preferred) or tomorrow, or null when neither day is a public
    /// holiday or the lookup fails.
    /// </summary>
    /// <param name="countryCode">ISO 3166-1 alpha-2 country code, e.g. "CH".</param>
    Task<UpcomingHoliday?> GetUpcomingHolidayAsync(string countryCode, CancellationToken cancellationToken = default);
}
