// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Provides the company's current calendar date resolved through its configured time zone, so business
/// dates (e.g. membership ValidFrom) reflect the operator's local day instead of the server's UTC day.
/// The returned value is the local date marked as UTC-midnight (DateTimeKind.Utc) for consistent storage
/// in timestamptz columns, matching how a user-typed date is persisted.
/// </summary>

namespace Klacks.Api.Domain.Interfaces.Settings;

public interface ICompanyClock
{
    /// <summary>
    /// Returns the company's current calendar date (local day per the configured time zone) as a
    /// UTC-midnight <see cref="DateTime"/> with <see cref="DateTimeKind.Utc"/>.
    /// </summary>
    Task<DateTime> GetTodayAsync(CancellationToken cancellationToken = default);
}
