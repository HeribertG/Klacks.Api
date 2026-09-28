// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Authentification;

namespace Klacks.Api.Domain.Interfaces.Authentification;

public interface IOAuthClientRepository
{
    Task<OAuthClient?> GetByClientIdAsync(string clientId, CancellationToken cancellationToken = default);

    Task AddAsync(OAuthClient client, CancellationToken cancellationToken = default);
}
