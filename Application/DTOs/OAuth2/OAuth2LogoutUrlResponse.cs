// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.OAuth2;

public class OAuth2LogoutUrlResponse
{
    public string? LogoutUrl { get; set; }
    public bool SupportsLogout { get; set; }
}
