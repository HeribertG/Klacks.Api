// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Models.Authentification;

public record OAuthAuthorizationCodeData(
    string UserId,
    string ClientId,
    string ClientName,
    string RedirectUri,
    string CodeChallenge,
    string? Scope);
