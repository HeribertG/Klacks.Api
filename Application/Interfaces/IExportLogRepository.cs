// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Exports;

namespace Klacks.Api.Application.Interfaces;

/// <summary>
/// Repository for ExportLog reads and writes.
/// </summary>
public interface IExportLogRepository
{
    Task AddAsync(ExportLog entry, CancellationToken cancellationToken = default);

    /// <summary>Returns the non-deleted export log entry with this id, or null when none exists.</summary>
    /// <param name="id">Id of the export log entry</param>
    Task<ExportLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<List<ExportLog>> GetRangeAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default);
}
