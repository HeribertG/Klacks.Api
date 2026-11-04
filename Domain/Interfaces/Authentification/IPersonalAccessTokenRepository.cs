// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Authentification;

namespace Klacks.Api.Domain.Interfaces.Authentification;

public interface IPersonalAccessTokenRepository
{
    Task<PersonalAccessToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    Task<List<PersonalAccessToken>> GetByUserAsync(string userId, CancellationToken cancellationToken = default);

    Task AddAsync(PersonalAccessToken token, CancellationToken cancellationToken = default);

    Task<PersonalAccessToken?> RevokeAsync(Guid id, string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft-deletes every personal access token of the user, including the OAuth access tokens issued as
    /// personal access tokens. Self-committing: calls SaveChangesAsync on the shared context, so it also flushes
    /// changes other repositories staged before; run it inside IUnitOfWork.ExecuteInTransactionAsync when it must
    /// be atomic with them.
    /// </summary>
    /// <param name="userId">Owner of the tokens</param>
    /// <param name="cancellationToken">Cancels the write</param>
    /// <returns>Number of tokens revoked</returns>
    Task<int> RevokeAllByUserAsync(string userId, CancellationToken cancellationToken = default);

    Task UpdateLastUsedAsync(Guid id, DateTime lastUsedAt, CancellationToken cancellationToken = default);
}
