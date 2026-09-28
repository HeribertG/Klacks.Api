// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.OAuth2;

public class OAuth2LogoutUrlResponse
{
    public string? LogoutUrl { get; set; }
    public bool SupportsLogout { get; set; }
}
