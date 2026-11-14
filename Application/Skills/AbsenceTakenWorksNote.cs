// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The sentence a skill adds when an absence takes uncovered services away.
/// </summary>

namespace Klacks.Api.Application.Skills;

internal static class AbsenceTakenWorksNote
{
    /// <summary>
    /// Builds the note that tells how many uncovered services were removed; empty for none.
    /// </summary>
    /// <param name="count">Services the absence removed</param>
    public static string Removed(int count)
        => count == 0 ? string.Empty : $" {count} uncovered service(s) removed.";

    /// <summary>
    /// Builds the note that tells how many uncovered services would be removed; empty for none.
    /// </summary>
    /// <param name="count">Services the absence would remove</param>
    public static string WouldRemove(int count)
        => count == 0 ? string.Empty : $" {count} uncovered service(s) would be removed.";
}
