// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Loads the identifiers of sealed orders (Shift.Status = SealedOrder) that own closed
/// work entries within a date range, for range-based exports.
/// </summary>
namespace Klacks.Api.Application.Interfaces.Exports;

public interface ISealedOrderIdLoader
{
    Task<List<Guid>> LoadIdsForRangeAsync(DateOnly fromDate, DateOnly untilDate, CancellationToken cancellationToken = default);
}
