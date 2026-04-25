// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.Constants;

public static class MyVersion
{
    public static string Variant { get; set; } = BuildVariantConstant.CVar;

    public static int Major => VersionConstant.CMajor;

    public static int Minor => VersionConstant.CMinor;

    public static int Patch => VersionConstant.CPatch;

    public static string BuildKey => VersionConstant.CBuildKey;

    public static string BuildTimestamp => VersionConstant.CBuildTimestamp;

    public static string Get(bool includeBuildInformations = false)
    {
        string version = BuildVariantConstant.CVar + "-" + VersionConstant.CMajor + "." + VersionConstant.CMinor + "." + VersionConstant.CPatch;
        if (includeBuildInformations)
        {
            var timestamp = DateTime.TryParse(VersionConstant.CBuildTimestamp, out var d) ? d.ToString("dd.MM.yyyy") : VersionConstant.CBuildTimestamp;
            version += " (" + VersionConstant.CBuildKey + " / " + timestamp + ")";
        }

        return version;
    }
}
