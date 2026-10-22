// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.Interfaces.Assistant;

/// <summary>
/// Runs the open-order summary detector once, outside the hourly tick, so orders an ERP import has just created
/// are reported right away instead of up to an hour later. It goes through the same pipeline as the tick (ledger,
/// rising-count dedup): a planner whose count of visible open orders rose above the count of the last summary they
/// received today gets a new summary with the new numbers; the same or a lower count sends nothing. Never throws
/// except for cancellation: a failed rescan must not fail the import.
/// </summary>
public interface IOpenOrderSummaryRescan
{
    /// <param name="cancellationToken">Cancels the rescan.</param>
    Task RunAsync(CancellationToken cancellationToken = default);
}
