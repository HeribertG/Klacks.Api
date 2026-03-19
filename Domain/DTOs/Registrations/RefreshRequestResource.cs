// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.DTOs.Registrations;

public class RefreshRequestResource
{
    public string Token { get; set; } = string.Empty;

    public string RefreshToken { get; set; } = string.Empty;
}
