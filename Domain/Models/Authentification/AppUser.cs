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
    /// User administration list display order. No longer editable from any UI (the user
    /// administration drag'n'drop was removed, 17.08.) - kept as a frozen snapshot rather than
    /// dropped outright, since removing the column is a bigger, unrequested change.
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Escalation roster's own wake-up order - deliberately separate from DisplayOrder so that
    /// dragging in the escalation roster can never affect the user administration list or vice
    /// versa (Owner decision, 17.08.: the two lists must not share storage). Backfilled on migration
    /// to reproduce the prior alphabetical order; new users are appended at the end.
    /// </summary>
    public int EscalationRosterOrder { get; set; }
}
