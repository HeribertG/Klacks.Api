// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Authentification;

namespace Klacks.Api.Domain.Interfaces.Authentification;

public interface IOAuthAuthorizationCodeStore
{
    Task StoreAsync(string code, OAuthAuthorizationCodeData data, CancellationToken cancellationToken = default);

    Task<OAuthAuthorizationCodeData?> ConsumeAsync(string code, CancellationToken cancellationToken = default);
}
