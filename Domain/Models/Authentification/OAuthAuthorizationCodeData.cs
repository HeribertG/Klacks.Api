// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Authentification;

public record OAuthAuthorizationCodeData(
    string UserId,
    string ClientId,
    string RedirectUri,
    string CodeChallenge,
    PersonalAccessTokenAccessMode AccessMode);
