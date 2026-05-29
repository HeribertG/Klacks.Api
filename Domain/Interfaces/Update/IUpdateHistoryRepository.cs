// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Update;

namespace Klacks.Api.Domain.Interfaces.Update;

public interface IUpdateHistoryRepository
{
    Task<UpdateHistory> AddAsync(UpdateHistory entry, CancellationToken cancellationToken = default);

    Task UpdateAsync(UpdateHistory entry, CancellationToken cancellationToken = default);

    Task<UpdateHistory?> GetActiveOperationAsync(CancellationToken cancellationToken = default);

    Task<UpdateHistory?> GetLastSuccessfulUpdateAsync(CancellationToken cancellationToken = default);

    Task<UpdateHistory?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UpdateHistory>> GetRecentAsync(int take, CancellationToken cancellationToken = default);
}
