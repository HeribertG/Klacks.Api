// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Maps a region-setup industry slug (an industryProfiles key, already slug-normalized) to the
/// QualificationCategory the K20 qualification-catalog import assigns. Extracted from the region
/// setup service so selection filtering can reuse the exact same mapping; any slug without an
/// explicit mapping resolves to Others.
/// </summary>

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Constants;

public static class IndustryQualificationCategoryMap
{
    private const string SpitexAlias = "spitex";
    private const string LogistikAlias = "logistik";
    private const string SpitaelerAlias = "spitaeler";
    private const string HospitalsAlias = "hospitals";
    private const string GastronomySlug = "gastronomy";
    private const string GastroAlias = "gastro";
    private const string ConstructionSlug = "construction";
    private const string CleaningSlug = "cleaning";
    private const string TransportSlug = "transport";

    public static QualificationCategory Resolve(string industrySlug) => industrySlug switch
    {
        IndustrySlugs.Homecare or SpitexAlias => QualificationCategory.Spitex,
        IndustrySlugs.Security => QualificationCategory.Security,
        IndustrySlugs.Logistics or LogistikAlias => QualificationCategory.Logistics,
        IndustrySlugs.Healthcare or SpitaelerAlias or HospitalsAlias => QualificationCategory.Healthcare,
        GastronomySlug or GastroAlias => QualificationCategory.Gastronomy,
        ConstructionSlug => QualificationCategory.Construction,
        IndustrySlugs.Facility or CleaningSlug => QualificationCategory.Cleaning,
        TransportSlug => QualificationCategory.Transport,
        _ => QualificationCategory.Others,
    };
}
