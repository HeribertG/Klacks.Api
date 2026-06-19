// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Interfaces.Schedules;

/// <summary>
/// Repository for SealedDay reads and writes.
/// </summary>
public interface ISealedDayRepository
{
    Task AddAsync(SealedDay entry, CancellationToken cancellationToken = default);

    Task<List<SealedDay>> GetRangeAsync(DateOnly from, DateOnly to, Guid? groupId, CancellationToken cancellationToken = default);

    Task<int> SoftDeleteRangeAsync(DateOnly from, DateOnly to, Guid? groupId, string deletedBy, CancellationToken cancellationToken = default);

    Task<bool> IsDayLockedAsync(DateOnly date, Guid clientId, CancellationToken cancellationToken = default);
}
