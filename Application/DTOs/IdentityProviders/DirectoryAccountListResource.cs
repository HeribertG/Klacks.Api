// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.IdentityProviders;

/// <summary>
/// The directory approval list of one provider.
/// </summary>
public class DirectoryAccountListResource
{
    /// <summary>False when the directory could not be read; Accounts is empty then.</summary>
    public bool Success { get; set; }

    /// <summary>True when the directory returned more accounts than the list shows.</summary>
    public bool IsTruncated { get; set; }

    public List<DirectoryAccountResource> Accounts { get; set; } = [];
}
