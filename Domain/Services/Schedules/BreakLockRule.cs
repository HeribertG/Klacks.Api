// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Services.Schedules;

/// <summary>
/// The lock rule of a stored break: a Closed break may only be written by an admin, and a sealed break may only be deleted
/// by an admin. A break of a scenario is never locked, as with ParentWorkLockGuard. The one definition for the break update
/// handler, the break children of a container save and the delete of a container with its break children.
/// </summary>
public static class BreakLockRule
{
    public const string ClosedBreakMessage = "Cannot modify a closed break entry.";

    public const string SealedBreakDeleteMessage = "Cannot delete a sealed break entry.";

    /// <summary>Throws InvalidRequestException when the caller may not write the stored break.</summary>
    /// <param name="stored">The break as stored; null when there is none (nothing to refuse)</param>
    /// <param name="isAdmin">Whether the caller has the Admin role</param>
    public static void EnsureWritable(Break? stored, bool isAdmin)
    {
        if (stored is { AnalyseToken: null, LockLevel: WorkLockLevel.Closed } && !isAdmin)
        {
            throw new InvalidRequestException(ClosedBreakMessage);
        }
    }

    /// <summary>Throws InvalidRequestException when the caller may not delete the stored break: any sealed break needs an admin.</summary>
    /// <param name="stored">The break as stored; null when there is none (nothing to refuse)</param>
    /// <param name="isAdmin">Whether the caller has the Admin role</param>
    public static void EnsureDeletable(Break? stored, bool isAdmin)
    {
        if (stored is { AnalyseToken: null } && stored.LockLevel != WorkLockLevel.None && !isAdmin)
        {
            throw new InvalidRequestException(SealedBreakDeleteMessage);
        }
    }
}
