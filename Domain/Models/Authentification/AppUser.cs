// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Microsoft.AspNetCore.Identity;

namespace Klacks.Api.Domain.Models.Authentification;

public class AppUser : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string? PasswordResetToken { get; set; }
    public DateTime? PasswordResetTokenExpires { get; set; }

    /// <summary>
    /// When set, the account was deliberately deactivated and must neither authenticate nor lend
    /// its permissions to background work. Deliberately not LockoutEnd: that stays the temporary
    /// block after failed sign-in attempts and is cleared automatically.
    /// </summary>
    public DateTime? DeactivatedAt { get; set; }

    /// <summary>User id (GUID string) of the account that performed the deactivation.</summary>
    public string? DeactivatedBy { get; set; }

    /// <summary>
    /// Shared display/call order: drives both the user administration list (drag'n'drop) and the
    /// escalation roster's wake-up order, so the two never drift apart. Backfilled on migration to
    /// reproduce the prior alphabetical order; new users are appended at the end.
    /// </summary>
    public int DisplayOrder { get; set; }
}
