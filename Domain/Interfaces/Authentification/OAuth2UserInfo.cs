// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Interfaces.Authentification;

public class OAuth2UserInfo
{
    public string? Id { get; set; }
    public string? Email { get; set; }
    public string? Name { get; set; }
    public string? GivenName { get; set; }
    public string? FamilyName { get; set; }
    public string? Picture { get; set; }
}
