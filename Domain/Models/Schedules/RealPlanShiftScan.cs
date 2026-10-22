// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Schedules;

/// <summary>
/// Which shifts of the real plan a scan reads: never a scenario row (AnalyseToken or ScenarioSourceShiftId set) and
/// never a soft-deleted one. Without an order flag the rows come in the base order (original id, from date,
/// start time).
/// </summary>
/// <param name="Status">The one status the shifts must have</param>
public sealed record RealPlanShiftScan(ShiftStatus Status)
{
    /// <summary>Only shifts of this type; null = every type.</summary>
    public ShiftType? ShiftType { get; init; }

    /// <summary>Only shifts starting on or after this day.</summary>
    public DateOnly? StartsOnOrAfter { get; init; }

    /// <summary>Only shifts without an end or ending on or after this day.</summary>
    public DateOnly? NotEndedBefore { get; init; }

    /// <summary>Only full-day shifts: equal start and end time (TimeRange.ForWorkingTime's 24 hour convention).</summary>
    public bool FullDayOnly { get; init; }

    /// <summary>Only these shifts; null = no id restriction.</summary>
    public IReadOnlyCollection<Guid>? OnlyIds { get; init; }

    /// <summary>Never these shifts; null = no exclusion.</summary>
    public IReadOnlyCollection<Guid>? ExcludedIds { get; init; }

    /// <summary>Sorts by from date, then id, in the database instead of the base order.</summary>
    public bool OrderByFromDateThenId { get; init; }

    /// <summary>Upper bound of rows, applied in the database after the order; null = all rows.</summary>
    public int? MaxCount { get; init; }
}
