// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Loads and assembles export data from the database for the supplied sealed orders.
/// @param orderIds - Identifiers of sealed-order shifts to export
/// @param fromDate - Optional lower bound on Work.CurrentDate
/// @param untilDate - Optional upper bound on Work.CurrentDate
/// </summary>
using Klacks.Api.Domain.Models.Exports;

namespace Klacks.Api.Application.Interfaces.Exports;

public interface IOrderExportDataLoader
{
    Task<OrderExportData> LoadAsync(
        IReadOnlyCollection<Guid> orderIds,
        DateOnly? fromDate = null,
        DateOnly? untilDate = null,
        CancellationToken cancellationToken = default);
}
