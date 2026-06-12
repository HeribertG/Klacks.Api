// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Authentification;

namespace Klacks.Api.Domain.Interfaces.Authentification;

public interface IOAuthAuthorizationCodeStore
{
    void Store(string code, OAuthAuthorizationCodeData data);

    OAuthAuthorizationCodeData? Consume(string code);
}
