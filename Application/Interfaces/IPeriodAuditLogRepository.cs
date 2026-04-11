// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.Interfaces;

/// <summary>
/// Repository for PeriodAuditLog reads and writes.
/// </summary>
public interface IPeriodAuditLogRepository
{
    Task AddAsync(PeriodAuditLog entry, CancellationToken cancellationToken = default);

    Task<List<PeriodAuditLog>> GetRangeAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken = default);
}
