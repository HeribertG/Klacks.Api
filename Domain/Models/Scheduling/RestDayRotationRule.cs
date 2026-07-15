// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// A statutory rest-day rotation rule (K10): at least <see cref="MinFreeCount"/> occurrences of
/// <see cref="DayOfWeek"/> must be work-free within any trailing window covering
/// <see cref="WindowWeeks"/> occurrences of that weekday (e.g. "every 2nd Sunday free" = 2 free Sundays
/// in 4 weeks, CH; "2 free Sundays per month", FR). Global (not per-client) — several rules can coexist.
/// Rows are written exclusively by the region-setup entity-import mechanism (K20);
/// ImportSourceKey/ImportContentHash (see <see cref="Klacks.Api.Domain.Common.IImportableEntity"/>)
/// drive that re-apply logic.
/// </summary>

using Klacks.Api.Domain.Common;

namespace Klacks.Api.Domain.Models.Scheduling;

public class RestDayRotationRule : BaseEntity, IImportableEntity
{
    public DayOfWeek DayOfWeek { get; set; }

    public int MinFreeCount { get; set; }

    public int WindowWeeks { get; set; }

    public string ImportSourceKey { get; set; } = string.Empty;

    public string ImportContentHash { get; set; } = string.Empty;
}
