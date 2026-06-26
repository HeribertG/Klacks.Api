// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IScheduledTaskRepository
{
    Task<List<ScheduledTask>> GetDueAsync(DateTime nowUtc, CancellationToken cancellationToken = default);

    Task<List<ScheduledTask>> GetByOwnerAsync(Guid ownerUserId, bool includeDisabled, CancellationToken cancellationToken = default);

    Task<ScheduledTask?> GetByOwnerAndNameAsync(Guid ownerUserId, string name, CancellationToken cancellationToken = default);

    Task<ScheduledTask?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically advances the next-run timestamp only if it still matches the expected value. Returns
    /// true when this caller won the claim, so a long run or a second API instance cannot double-fire
    /// the same occurrence.
    /// </summary>
    Task<bool> TryClaimAsync(Guid id, DateTime? expectedNextRunUtc, DateTime? newNextRunUtc, CancellationToken cancellationToken = default);

    Task AddAsync(ScheduledTask task, CancellationToken cancellationToken = default);

    Task UpdateAsync(ScheduledTask task, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
