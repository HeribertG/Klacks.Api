// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Bots;

namespace Klacks.Api.Domain.Interfaces.Bots;

public interface IKlacksBotTokenRepository
{
    Task<KlacksBotToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    Task<List<KlacksBotToken>> GetAllAsync(CancellationToken cancellationToken = default);

    Task AddAsync(KlacksBotToken token, CancellationToken cancellationToken = default);

    Task<KlacksBotToken?> RevokeAsync(Guid id, CancellationToken cancellationToken = default);

    Task UpdateLastUsedAsync(Guid id, DateTime lastUsedAt, CancellationToken cancellationToken = default);
}
