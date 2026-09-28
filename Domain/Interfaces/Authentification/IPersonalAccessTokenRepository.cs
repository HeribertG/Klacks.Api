// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Authentification;

namespace Klacks.Api.Domain.Interfaces.Authentification;

public interface IPersonalAccessTokenRepository
{
    Task<PersonalAccessToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    Task<List<PersonalAccessToken>> GetByUserAsync(string userId, CancellationToken cancellationToken = default);

    Task AddAsync(PersonalAccessToken token, CancellationToken cancellationToken = default);

    Task<PersonalAccessToken?> RevokeAsync(Guid id, string userId, CancellationToken cancellationToken = default);

    Task UpdateLastUsedAsync(Guid id, DateTime lastUsedAt, CancellationToken cancellationToken = default);
}
