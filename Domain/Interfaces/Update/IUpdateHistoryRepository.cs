// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Update;

namespace Klacks.Api.Domain.Interfaces.Update;

public interface IUpdateHistoryRepository
{
    Task<UpdateHistory> AddAsync(UpdateHistory entry, CancellationToken cancellationToken = default);

    Task UpdateAsync(UpdateHistory entry, CancellationToken cancellationToken = default);

    Task DeleteAsync(UpdateHistory entry, CancellationToken cancellationToken = default);

    Task<UpdateHistory?> GetActiveOperationAsync(CancellationToken cancellationToken = default);

    Task<UpdateHistory?> GetLastSuccessfulUpdateAsync(CancellationToken cancellationToken = default);

    Task<UpdateHistory?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UpdateHistory>> GetRecentAsync(int take, CancellationToken cancellationToken = default);

    Task<UpdateHistory?> GetLatestByTypesAsync(IReadOnlyCollection<UpdateOperationType> types, CancellationToken cancellationToken = default);
}
