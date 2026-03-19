// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.DTOs.IdentityProviders;

public class TestConnectionResultResource
{
    public bool Success { get; set; }

    public string? ErrorMessage { get; set; }

    public int? UserCount { get; set; }

    public List<string>? SampleUsers { get; set; }
}
