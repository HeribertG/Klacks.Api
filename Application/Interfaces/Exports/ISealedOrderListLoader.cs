// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Loads the list of sealed orders available for export, joined with customer
/// data and Work-locking statistics. Used by the export selection dropdown.
/// </summary>
using Klacks.Api.Application.DTOs.Exports;

namespace Klacks.Api.Application.Interfaces.Exports;

public interface ISealedOrderListLoader
{
    Task<List<SealedOrderListItem>> LoadAsync(
        DateOnly? fromDate,
        DateOnly? untilDate,
        Guid? customerId,
        string? searchTerm,
        CancellationToken cancellationToken = default);
}
