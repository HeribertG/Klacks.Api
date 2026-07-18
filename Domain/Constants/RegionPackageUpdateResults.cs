// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Constants;

public static class RegionPackageUpdateResults
{
    public const string UpToDate = "upToDate";
    public const string PackageNotFound = "packageNotFound";
    public const string Updated = "updated";
    public const string UpdateAvailableAutoOff = "updateAvailableAutoOff";
    public const string BlockedByMinVersion = "blockedByMinVersion";
    public const string Conflict = "conflict";
    public const string Error = "error";
}
