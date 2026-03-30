// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Loads and assembles export data from the database, resolving order relationships.
/// @param startDate - Start of the export period
/// @param endDate - End of the export period
/// </summary>
using Klacks.Api.Domain.Models.Exports;

namespace Klacks.Api.Application.Interfaces.Exports;

public interface IOrderExportDataLoader
{
    Task<OrderExportData> LoadAsync(DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default);
}
