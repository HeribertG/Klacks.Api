// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Pre-geocoded street coordinates for seed data generation.
/// Generated via Nominatim geocoding of all seed street names.
/// </summary>

namespace Klacks.Api.Data.Seed.Generators;

public static class StreetCoordinateGenerator
{
    private static readonly Dictionary<string, (double Latitude, double Longitude)> StreetCoordinates = new()
    {
        { "Aeschengraben|Basel", (47.5506117, 7.5940340) },
        { "Aeschenvorstadt|Basel", (47.5520205, 7.5940267) },
        { "Centralbahnstrasse|Basel", (47.5480919, 7.5891649) },
        { "Elisabethenstrasse|Basel", (47.5536949, 7.5921119) },
        { "Freie Strasse|Basel", (47.5573635, 7.5888128) },
        { "Gerbergasse|Basel", (47.5571463, 7.5881839) },
        { "Spalenberg|Basel", (47.5572748, 7.5864487) },
        { "Steinenberg|Basel", (47.5540769, 7.5894818) },
    };

    public static (double Latitude, double Longitude)? GetCoordinates(string streetName, string city)
    {
        var key = "${streetName}|${city}";
        if (StreetCoordinates.TryGetValue(key, out var coords))
        {
            return coords;
        }

        var townKey = "_TOWN|${city}";
        if (StreetCoordinates.TryGetValue(townKey, out var townCoords))
        {
            return townCoords;
        }

        return null;
    }
}

