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

    /// <summary>
    /// Returns true when at least one non-deleted export log entry overlaps the given period,
    /// taking the optional group scope into account.
    /// </summary>
    /// <remarks>
    /// A caller querying a specific <paramref name="groupId"/> is considered "already exported"
    /// if either a group-specific export for that group or a global-scope export (GroupId == null)
    /// exists for the overlapping period. This is intentional: global exports cover every group
    /// by construction, and a later unseal should warn the admin even if no group-specific
    /// export exists. Do not simplify the three-way null handling without updating the warning
    /// logic in the Period Closing handlers.
    /// </remarks>
    Task<bool> HasExportForPeriodAsync(DateOnly startDate, DateOnly endDate, Guid? groupId, CancellationToken cancellationToken = default);
}
