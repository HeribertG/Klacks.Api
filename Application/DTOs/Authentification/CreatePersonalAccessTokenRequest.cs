// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Authentification;

public class CreatePersonalAccessTokenRequest
{
    public string Name { get; set; } = string.Empty;

    public int? ExpiresInDays { get; set; }
}
