// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Structure of the ImportSourceKey namespace the region-setup importer writes for rows that belong to
/// one industryProfiles block: "region-setup:industryProfiles:{industry}:{entityKind}:{slug}". The
/// industry segment is the slug-normalized industryProfiles map key, which is why the same preset of
/// two different industries differs only in that one segment - the property this file exists for, since
/// it makes presets comparable across industries.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class RegionSetupImportKeys
{
    public const string IndustryProfilesPrefix = "region-setup:industryProfiles:";

    public const char SegmentSeparator = ':';
}
