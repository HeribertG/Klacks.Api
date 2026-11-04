// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Outcome of a zip-bomb check: within limits, or the first violated limit with an English message.
/// </summary>
/// <param name="Violation">The violated limit; None when the archive is within all limits</param>
/// <param name="Message">English description of the violation; empty when within limits</param>
/// <param name="Cause">Library failure behind an Unreadable verdict; only for logging</param>
namespace Klacks.Api.Infrastructure.Services.Archives;

public sealed record ZipArchiveLimitCheck(ZipArchiveLimitViolation Violation, string Message, Exception? Cause = null)
{
    public bool IsWithinLimits => Violation == ZipArchiveLimitViolation.None;

    public static ZipArchiveLimitCheck WithinLimits() => new(ZipArchiveLimitViolation.None, string.Empty);
}
