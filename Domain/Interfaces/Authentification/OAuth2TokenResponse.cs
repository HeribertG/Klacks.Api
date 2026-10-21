// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Authentification;

namespace Klacks.Api.Domain.Interfaces.Authentification;

public class OAuth2TokenResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string? RefreshToken { get; set; }
    public string? TokenType { get; set; }
    public int? ExpiresIn { get; set; }
    public string? IdToken { get; set; }
}
