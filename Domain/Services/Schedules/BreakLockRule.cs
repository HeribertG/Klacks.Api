// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Services.Schedules;

/// <summary>
/// The lock rule of a stored break: a Closed break may only be written by an admin. The one definition for the break
/// update handler and for the break children of a container save.
/// </summary>
public static class BreakLockRule
{
    public const string ClosedBreakMessage = "Cannot modify a closed break entry.";

    /// <summary>Throws InvalidRequestException when the caller may not write the stored break.</summary>
    /// <param name="stored">The break as stored; null when there is none (nothing to refuse)</param>
    /// <param name="isAdmin">Whether the caller has the Admin role</param>
    public static void EnsureWritable(Break? stored, bool isAdmin)
    {
        if (stored?.LockLevel == WorkLockLevel.Closed && !isAdmin)
        {
            throw new InvalidRequestException(ClosedBreakMessage);
        }
    }
}
