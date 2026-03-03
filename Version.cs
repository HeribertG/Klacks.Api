// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Infrastructure.Constants;

namespace Klacks.Api;

public class MyVersion : VersionConstant
{
    public static string Variant { get; set; } = BuildVariantConstant.CVar;

    public static int Major => CMajor;

    public static int Minor => CMinor;

    public static int Patch => CPatch;

    public static string BuildKey => CBuildKey;

    public static string BuildTimestamp => CBuildTimestamp;

    public string Get(bool includeBuildInformations = false)
    {
        string version = BuildVariantConstant.CVar + "-" + CMajor + "." + CMinor + "." + CPatch;
        if (includeBuildInformations)
        {
            var d = DateTime.Parse(CBuildTimestamp);
            version += " (" + CBuildKey + " / " + d.ToString("dd.MM.yyyy") + ")";
        }

        return version;
    }
}
