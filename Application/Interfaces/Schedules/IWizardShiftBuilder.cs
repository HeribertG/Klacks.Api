// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.ScheduleOptimizer.Models;

namespace Klacks.Api.Application.Interfaces.Schedules;

/// <summary>
/// Expands shift definitions to per-day slots for the wizard period.
/// For each regular shift active on a given weekday the builder emits Quantity x SumEmployees single-seat
/// CoreShift instances per date; sporadic shifts have no daily demand and are not emitted.
/// </summary>
public interface IWizardShiftBuilder
{
    /// <summary>
    /// Enumerates all shift slots active within the given period.
    /// </summary>
    /// <param name="shiftIds">Optional subset of shifts; null = all shifts that overlap the period</param>
    /// <param name="from">Period start (inclusive)</param>
    /// <param name="until">Period end (inclusive)</param>
    /// <param name="analyseToken">null = real-mode shifts; set = clones of an active scenario</param>
    /// <param name="ct">Cancellation token</param>
    Task<IReadOnlyList<CoreShift>> BuildAsync(
        IReadOnlyList<Guid>? shiftIds,
        DateOnly from,
        DateOnly until,
        Guid? analyseToken,
        CancellationToken ct);
}
