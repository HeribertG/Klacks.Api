// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Provides the company's current time resolved through its configured time zone - the single
/// installation-wide time zone (never a hard-coded regional default, never the server's local zone,
/// never UTC-as-a-day), so business dates and "now" reflect the operator's own local day and clock
/// instead of the server's UTC day. <see cref="GetTodayAsync"/> keeps its legacy shape (the local date
/// marked as UTC-midnight <see cref="DateTimeKind.Utc"/>) for existing timestamptz callers;
/// <see cref="GetTodayDateAsync"/> is the calendar-clean equivalent for callers already on
/// <see cref="DateOnly"/>. All methods resolve the same configured zone and are memoised per DI scope,
/// invalidated when <see cref="ISettingsChangeVersion"/> advances - never across scopes.
/// </summary>

using Klacks.Api.Domain.Models.Settings;

namespace Klacks.Api.Domain.Interfaces.Settings;

public interface ICompanyClock
{
    /// <summary>
    /// Returns the company's current calendar date (local day per the configured time zone) as a
    /// UTC-midnight <see cref="DateTime"/> with <see cref="DateTimeKind.Utc"/>.
    /// </summary>
    Task<DateTime> GetTodayAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the company's current calendar date (local day per the configured time zone) as a
    /// plain <see cref="DateOnly"/>, with no time-zone-ambiguous <see cref="DateTime.Kind"/> to misread.
    /// </summary>
    Task<DateOnly> GetTodayDateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the current instant as a <see cref="DateTimeOffset"/> carrying the company's configured
    /// time zone offset, derived from the injected <see cref="TimeProvider"/> for deterministic testing.
    /// </summary>
    Task<DateTimeOffset> GetNowAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves the company's configured time zone (explicit APP_ADDRESS_TIMEZONE setting, else the
    /// zone derived from APP_ADDRESS_COUNTRY, else the zone derived from the global calendar's country
    /// setting (SettingKeys.GlobalCalendarCountry), else UTC as the neutral fallback).
    /// </summary>
    Task<TimeZoneInfo> GetTimeZoneAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves the company's configured time zone together with which step of the resolution chain
    /// produced it. Uses the same resolution and per-scope memo as <see cref="GetTimeZoneAsync"/> - no
    /// second resolution pass.
    /// </summary>
    Task<CompanyTimeZoneResolution> GetTimeZoneResolutionAsync(CancellationToken cancellationToken = default);
}
