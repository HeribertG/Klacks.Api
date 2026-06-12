// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Authentification;

namespace Klacks.Api.Domain.Interfaces.Authentification;

public interface IOAuthClientRepository
{
    Task<OAuthClient?> GetByClientIdAsync(string clientId, CancellationToken cancellationToken = default);

    Task AddAsync(OAuthClient client, CancellationToken cancellationToken = default);
}
