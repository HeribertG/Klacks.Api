// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Loads the list of sealed orders available for export, joined with customer
/// data and Work-locking statistics. Used by the export selection dropdown.
/// </summary>
/// <param name="visibleGroupIds">Shift group restriction of the caller from IShiftGroupFilterService.ResolveShiftListGroupIdsAsync,
/// applied before the result cap; null = unrestricted</param>
using Klacks.Api.Application.DTOs.Exports;

namespace Klacks.Api.Application.Interfaces.Exports;

public interface ISealedOrderListLoader
{
    Task<List<SealedOrderListItem>> LoadAsync(
        DateOnly? fromDate,
        DateOnly? untilDate,
        Guid? customerId,
        string? searchTerm,
        IReadOnlyCollection<Guid>? visibleGroupIds,
        CancellationToken cancellationToken = default);
}
