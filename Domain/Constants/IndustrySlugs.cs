// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// The five canonical industry slugs all shipped region-setup content is written against
/// (industryProfiles keys, ACTIVE_INDUSTRIES setting values, SchedulingRule.Industry).
/// The importer technically accepts any slug; these constants keep shipped profiles and
/// country packs comparable across the product line.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class IndustrySlugs
{
    public const string Homecare = "homecare";
    public const string Healthcare = "healthcare";
    public const string Security = "security";
    public const string Facility = "facility";
    public const string Logistics = "logistics";

    /// <summary>
    /// Marker value for the ACTIVE_INDUSTRIES setting: the installation runs exclusively on its
    /// own custom scheduling rules, no shipped industry profile is active. Deliberately excluded
    /// from <see cref="All"/> since it is not an industry that can occur on
    /// SchedulingRule.Industry/Qualification.Industry or as an industryProfiles map key.
    /// </summary>
    public const string Custom = "custom";

    public static readonly IReadOnlyList<string> All = new[]
    {
        Homecare,
        Healthcare,
        Security,
        Facility,
        Logistics,
    };
}
