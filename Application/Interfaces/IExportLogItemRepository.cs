// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Exports;
using Klacks.Api.Domain.Models.Exports;

namespace Klacks.Api.Application.Interfaces;

/// <summary>
/// Repository for the per-person records of payroll exports. Stage-only: writes are committed by the caller
/// through IUnitOfWork, together with the ExportLog row of the same run.
/// </summary>
public interface IExportLogItemRepository
{
    /// <summary>Stages the items; nothing is committed here.</summary>
    /// <param name="items">The per-person records of one export run</param>
    Task AddRangeAsync(IEnumerable<ExportLogItem> items, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns, per person, the latest export (highest revision) for exactly this period and format.
    /// Persons without any export are absent from the result.
    /// </summary>
    /// <param name="from">Period start (exact bound)</param>
    /// <param name="until">Period end (exact bound)</param>
    /// <param name="format">Payroll format key</param>
    /// <param name="clientIds">Optional restriction to these persons; null covers everybody</param>
    Task<Dictionary<Guid, LatestExportedItem>> GetLatestItemsAsync(
        DateOnly from,
        DateOnly until,
        string format,
        IReadOnlyCollection<Guid>? clientIds,
        CancellationToken cancellationToken = default);
    /// <summary>
    /// Returns the items of these persons whose period overlaps the given period without having exactly its bounds.
    /// </summary>
    /// <param name="clientIds">The persons to check</param>
    /// <param name="from">Period start</param>
    /// <param name="until">Period end</param>
    Task<List<ExportLogItem>> GetOverlappingAsync(
        IReadOnlyCollection<Guid> clientIds,
        DateOnly from,
        DateOnly until,
        CancellationToken cancellationToken = default);
}
