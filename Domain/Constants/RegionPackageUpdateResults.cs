// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Constants;

public static class RegionPackageUpdateResults
{
    public const string UpToDate = "upToDate";
    public const string PackageNotFound = "packageNotFound";
    public const string Updated = "updated";
    public const string UpdateAvailableAutoOff = "updateAvailableAutoOff";
    public const string BlockedByMinVersion = "blockedByMinVersion";
    public const string Conflict = "conflict";
    public const string SignatureRejected = "signatureRejected";
    public const string Error = "error";
}
