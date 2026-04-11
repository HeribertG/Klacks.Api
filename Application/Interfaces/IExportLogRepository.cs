// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Exports;

namespace Klacks.Api.Application.Interfaces;

/// <summary>
/// Repository for ExportLog reads and writes.
/// </summary>
public interface IExportLogRepository
{
    Task AddAsync(ExportLog entry, CancellationToken cancellationToken = default);

    Task<List<ExportLog>> GetRangeAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default);

    Task<bool> HasExportForPeriodAsync(DateOnly startDate, DateOnly endDate, Guid? groupId, CancellationToken cancellationToken = default);
}
