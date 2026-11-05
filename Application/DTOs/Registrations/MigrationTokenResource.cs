// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Session answer of a refresh whose refresh token came from the request body, i.e. a client that still keeps the token in
/// local storage (browser tabs running the Ui bundle from before the HttpOnly cookie). Unlike <see cref="TokenResource"/>
/// it also returns the newly issued refresh token in the body, so such a tab can store it and keep refreshing until it
/// reloads; the cookie is set as well. Clients on the cookie never send a body token, so they never receive this shape.
/// Goes away together with the body migration path.
/// </summary>

using System.Text.Json.Serialization;

namespace Klacks.Api.Application.DTOs.Registrations;

public class MigrationTokenResource : TokenResource
{
    private const string RefreshTokenJsonName = "refreshToken";

    [JsonPropertyName(RefreshTokenJsonName)]
    public string LegacyClientRefreshToken => RefreshToken;
}
