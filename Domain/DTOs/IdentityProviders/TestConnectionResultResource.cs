// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.DTOs.IdentityProviders;

public class TestConnectionResultResource
{
    public bool Success { get; set; }

    public string? ErrorMessage { get; set; }

    public int? UserCount { get; set; }

    public List<string>? SampleUsers { get; set; }
}
