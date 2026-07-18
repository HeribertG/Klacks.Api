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

    public static readonly IReadOnlyList<string> All = new[]
    {
        Homecare,
        Healthcare,
        Security,
        Facility,
        Logistics,
    };
}
