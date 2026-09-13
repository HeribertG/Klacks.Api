// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Location and naming of the per-country region files that map a state code (canton, Bundesland,
/// département, province) to the region the state belongs to. One file per country, named by the
/// ISO country code, shipped with the application below the base directory.
/// </summary>
namespace Klacks.Api.Infrastructure.Services.Geo;

public static class CountryRegionFileLayout
{
    public const string Directory = "GeoData/Regions";

    public const string FileExtension = ".json";
}
