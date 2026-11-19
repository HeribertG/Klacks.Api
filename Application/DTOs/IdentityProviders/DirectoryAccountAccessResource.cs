// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.IdentityProviders;

/// <summary>
/// Request body of the approval change: grant or withdraw sign-in for one directory account.
/// </summary>
public class DirectoryAccountAccessResource
{
    public string ExternalId { get; set; } = string.Empty;

    public bool MayLogin { get; set; }
}
