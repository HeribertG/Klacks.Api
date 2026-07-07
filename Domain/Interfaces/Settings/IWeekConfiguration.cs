// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Resolves the operator's configured weekend days and first day of the week, so calendar views,
/// period boundaries and reports agree on what counts as "weekend" instead of each call site assuming
/// Saturday/Sunday and Monday. Falls back to Saturday/Sunday and Monday when no setting is stored, which
/// matches the historical hard-coded behavior for installs that never configure this.
/// </summary>

namespace Klacks.Api.Domain.Interfaces.Settings;

public interface IWeekConfiguration
{
    /// <summary>
    /// Returns the configured weekend days (0 to 7 days), or {Saturday, Sunday} when unconfigured.
    /// </summary>
    Task<IReadOnlySet<DayOfWeek>> GetWeekendDaysAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns whether the given day counts as a weekend day per the configured (or default) weekend days.
    /// </summary>
    Task<bool> IsWeekendAsync(DayOfWeek dayOfWeek, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the configured first day of the week, or Monday when unconfigured.
    /// </summary>
    Task<DayOfWeek> GetWeekStartDayAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the start date of the week (per the configured week start day) that contains the given date.
    /// </summary>
    Task<DateOnly> GetWeekStartAsync(DateOnly date, CancellationToken cancellationToken = default);
}
