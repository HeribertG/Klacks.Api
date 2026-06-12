// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Models.Authentification;

public record OAuthAuthorizationCodeData(
    string UserId,
    string ClientId,
    string ClientName,
    string RedirectUri,
    string CodeChallenge,
    string? Scope);
