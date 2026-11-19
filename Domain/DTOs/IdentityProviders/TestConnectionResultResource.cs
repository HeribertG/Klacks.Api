// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.DTOs.IdentityProviders;

public class TestConnectionResultResource
{
    public bool Success { get; set; }

    public string? ErrorMessage { get; set; }

    /// <summary>Number of enabled accounts the directory returned.</summary>
    public int? UserCount { get; set; }

    /// <summary>True when the directory stopped early (server size limit), so UserCount is a lower bound.</summary>
    public bool IsTruncated { get; set; }

    public List<string>? SampleUsers { get; set; }
}
