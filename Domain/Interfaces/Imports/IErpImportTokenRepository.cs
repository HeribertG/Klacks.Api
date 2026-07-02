// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Imports;

namespace Klacks.Api.Domain.Interfaces.Imports;

public interface IErpImportTokenRepository
{
    Task<ErpImportToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    Task<List<ErpImportToken>> GetByDropPointAsync(Guid dropPointId, CancellationToken cancellationToken = default);

    Task AddAsync(ErpImportToken token, CancellationToken cancellationToken = default);

    Task<ErpImportToken?> RevokeAsync(Guid id, Guid dropPointId, CancellationToken cancellationToken = default);

    Task UpdateLastUsedAsync(Guid id, DateTime lastUsedAt, CancellationToken cancellationToken = default);
}
