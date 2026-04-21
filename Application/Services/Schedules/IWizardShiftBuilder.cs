// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.ScheduleOptimizer.Models;

namespace Klacks.Api.Application.Services.Schedules;

/// <summary>
/// Expands shift definitions to per-day slots for the wizard period.
/// For each shift active on a given weekday the builder emits one CoreShift instance per date.
/// Quantity-based slot multiplication is deferred (Phase 3 responsibility).
/// </summary>
public interface IWizardShiftBuilder
{
    /// <summary>
    /// Enumerates all shift slots active within the given period.
    /// </summary>
    /// <param name="shiftIds">Optional subset of shifts; null = all shifts that overlap the period</param>
    /// <param name="from">Period start (inclusive)</param>
    /// <param name="until">Period end (inclusive)</param>
    /// <param name="ct">Cancellation token</param>
    Task<IReadOnlyList<CoreShift>> BuildAsync(
        IReadOnlyList<Guid>? shiftIds,
        DateOnly from,
        DateOnly until,
        CancellationToken ct);
}
