// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.IdentityProviders;

/// <summary>
/// One row of the directory approval list: a directory account and whether it may sign in to Klacks.
/// </summary>
public class DirectoryAccountResource
{
    /// <summary>Stable directory id (objectGUID, entryUUID or DN); the key of the approval change.</summary>
    public string ExternalId { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Name the person types into the login form (AD: sAMAccountName, LDAP: uid).</summary>
    public string LoginName { get; set; } = string.Empty;

    public string? Email { get; set; }

    /// <summary>True when a linked, not deactivated Klacks account exists.</summary>
    public bool MayLogin { get; set; }

    /// <summary>Id of the linked Klacks account; null when none.</summary>
    public string? UserId { get; set; }

    /// <summary>True when the entry is not linked and its e-mail is already used by a Klacks account; granting is refused.</summary>
    public bool EmailInUse { get; set; }

    /// <summary>True when the linked account holds at least one role.</summary>
    public bool HasRoles { get; set; }

    /// <summary>False for a linked account the directory no longer returns.</summary>
    public bool IsInDirectory { get; set; }
}
