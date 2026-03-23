// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Settings;

namespace Klacks.Api.Data.Seed.Generators;

public static class PostcodeGenerator
{
    public static List<PostcodeCH> GenerateComprehensiveSwissPostcodes()
    {
        return new List<PostcodeCH>
        {
            // Zurich
            new PostcodeCH { Zip = 8000, City = "Zürich", State = "ZH" },
            new PostcodeCH { Zip = 8001, City = "Zürich", State = "ZH" },
            new PostcodeCH { Zip = 8002, City = "Zürich", State = "ZH" },
            new PostcodeCH { Zip = 8003, City = "Zürich", State = "ZH" },
            new PostcodeCH { Zip = 8004, City = "Zürich", State = "ZH" },
            new PostcodeCH { Zip = 8005, City = "Zürich", State = "ZH" },
            new PostcodeCH { Zip = 8006, City = "Zürich", State = "ZH" },
            new PostcodeCH { Zip = 8008, City = "Zürich", State = "ZH" },
            new PostcodeCH { Zip = 8032, City = "Zürich", State = "ZH" },
            new PostcodeCH { Zip = 8037, City = "Zürich", State = "ZH" },
            new PostcodeCH { Zip = 8038, City = "Zürich", State = "ZH" },
            new PostcodeCH { Zip = 8041, City = "Zürich", State = "ZH" },
            new PostcodeCH { Zip = 8045, City = "Zürich", State = "ZH" },
            new PostcodeCH { Zip = 8046, City = "Zürich", State = "ZH" },
            new PostcodeCH { Zip = 8047, City = "Zürich", State = "ZH" },
            new PostcodeCH { Zip = 8048, City = "Zürich", State = "ZH" },
            new PostcodeCH { Zip = 8049, City = "Zürich", State = "ZH" },
            new PostcodeCH { Zip = 8050, City = "Zürich", State = "ZH" },
            new PostcodeCH { Zip = 8051, City = "Zürich", State = "ZH" },
            new PostcodeCH { Zip = 8052, City = "Zürich", State = "ZH" },
            new PostcodeCH { Zip = 8053, City = "Zürich", State = "ZH" },
            new PostcodeCH { Zip = 8055, City = "Zürich", State = "ZH" },
            new PostcodeCH { Zip = 8057, City = "Zürich", State = "ZH" },
            new PostcodeCH { Zip = 8064, City = "Zürich", State = "ZH" },
            new PostcodeCH { Zip = 8102, City = "Oberengstringen", State = "ZH" },
            new PostcodeCH { Zip = 8103, City = "Unterengstringen", State = "ZH" },
            new PostcodeCH { Zip = 8104, City = "Weiningen ZH", State = "ZH" },
            new PostcodeCH { Zip = 8105, City = "Regensdorf", State = "ZH" },
            new PostcodeCH { Zip = 8106, City = "Adlikon b. Regensdorf", State = "ZH" },

            // Bern
            new PostcodeCH { Zip = 3000, City = "Bern", State = "BE" },
            new PostcodeCH { Zip = 3001, City = "Bern", State = "BE" },
            new PostcodeCH { Zip = 3003, City = "Bern", State = "BE" },
            new PostcodeCH { Zip = 3004, City = "Bern", State = "BE" },
            new PostcodeCH { Zip = 3005, City = "Bern", State = "BE" },
            new PostcodeCH { Zip = 3006, City = "Bern", State = "BE" },
            new PostcodeCH { Zip = 3007, City = "Bern", State = "BE" },
            new PostcodeCH { Zip = 3008, City = "Bern", State = "BE" },
            new PostcodeCH { Zip = 3010, City = "Bern", State = "BE" },
            new PostcodeCH { Zip = 3011, City = "Bern", State = "BE" },
            new PostcodeCH { Zip = 3012, City = "Bern", State = "BE" },
            new PostcodeCH { Zip = 3013, City = "Bern", State = "BE" },
            new PostcodeCH { Zip = 3014, City = "Bern", State = "BE" },
            new PostcodeCH { Zip = 3015, City = "Bern", State = "BE" },
            new PostcodeCH { Zip = 3018, City = "Bern", State = "BE" },
            new PostcodeCH { Zip = 3027, City = "Bern", State = "BE" },
            new PostcodeCH { Zip = 3032, City = "Hinterkappelen", State = "BE" },

            // Basel
            new PostcodeCH { Zip = 4000, City = "Basel", State = "BS" },
            new PostcodeCH { Zip = 4001, City = "Basel", State = "BS" },
            new PostcodeCH { Zip = 4002, City = "Basel", State = "BS" },
            new PostcodeCH { Zip = 4051, City = "Basel", State = "BS" },
            new PostcodeCH { Zip = 4052, City = "Basel", State = "BS" },
            new PostcodeCH { Zip = 4053, City = "Basel", State = "BS" },
            new PostcodeCH { Zip = 4054, City = "Basel", State = "BS" },
            new PostcodeCH { Zip = 4055, City = "Basel", State = "BS" },
            new PostcodeCH { Zip = 4056, City = "Basel", State = "BS" },
            new PostcodeCH { Zip = 4057, City = "Basel", State = "BS" },
            new PostcodeCH { Zip = 4058, City = "Basel", State = "BS" },

            // Geneva
            new PostcodeCH { Zip = 1200, City = "Genève", State = "GE" },
            new PostcodeCH { Zip = 1201, City = "Genève", State = "GE" },
            new PostcodeCH { Zip = 1202, City = "Genève", State = "GE" },
            new PostcodeCH { Zip = 1203, City = "Genève", State = "GE" },
            new PostcodeCH { Zip = 1204, City = "Genève", State = "GE" },
            new PostcodeCH { Zip = 1205, City = "Genève", State = "GE" },
            new PostcodeCH { Zip = 1206, City = "Genève", State = "GE" },
            new PostcodeCH { Zip = 1207, City = "Genève", State = "GE" },
            new PostcodeCH { Zip = 1208, City = "Genève", State = "GE" },
            new PostcodeCH { Zip = 1209, City = "Genève", State = "GE" },

            // Lucerne
            new PostcodeCH { Zip = 6000, City = "Luzern", State = "LU" },
            new PostcodeCH { Zip = 6003, City = "Luzern", State = "LU" },
            new PostcodeCH { Zip = 6004, City = "Luzern", State = "LU" },
            new PostcodeCH { Zip = 6005, City = "Luzern", State = "LU" },
            new PostcodeCH { Zip = 6006, City = "Luzern", State = "LU" },

            // Lausanne
            new PostcodeCH { Zip = 1000, City = "Lausanne", State = "VD" },
            new PostcodeCH { Zip = 1003, City = "Lausanne", State = "VD" },
            new PostcodeCH { Zip = 1004, City = "Lausanne", State = "VD" },
            new PostcodeCH { Zip = 1005, City = "Lausanne", State = "VD" },
            new PostcodeCH { Zip = 1006, City = "Lausanne", State = "VD" },
            new PostcodeCH { Zip = 1007, City = "Lausanne", State = "VD" },
            new PostcodeCH { Zip = 1010, City = "Lausanne", State = "VD" },
            new PostcodeCH { Zip = 1011, City = "Lausanne", State = "VD" },
            new PostcodeCH { Zip = 1012, City = "Lausanne", State = "VD" },
        };
    }

    /// <summary>
    /// Returns approximate city-center coordinates for each Swiss postcode.
    /// </summary>
    public static Dictionary<int, (double Latitude, double Longitude)> GetPostcodeCoordinates()
    {
        return new Dictionary<int, (double Latitude, double Longitude)>
        {
            // Zurich
            { 8000, (47.3769, 8.5417) },
            { 8001, (47.3686, 8.5391) },
            { 8002, (47.3635, 8.5312) },
            { 8003, (47.3727, 8.5254) },
            { 8004, (47.3783, 8.5245) },
            { 8005, (47.3866, 8.5191) },
            { 8006, (47.3798, 8.5477) },
            { 8008, (47.3556, 8.5498) },
            { 8032, (47.3713, 8.5577) },
            { 8037, (47.3925, 8.5136) },
            { 8038, (47.3489, 8.5239) },
            { 8041, (47.3398, 8.5238) },
            { 8045, (47.3489, 8.5133) },
            { 8046, (47.4024, 8.4956) },
            { 8047, (47.3786, 8.4934) },
            { 8048, (47.3877, 8.4844) },
            { 8049, (47.4049, 8.5031) },
            { 8050, (47.4105, 8.5429) },
            { 8051, (47.4069, 8.5564) },
            { 8052, (47.4155, 8.5337) },
            { 8053, (47.3402, 8.5467) },
            { 8055, (47.3733, 8.5043) },
            { 8057, (47.3956, 8.5382) },
            { 8064, (47.3877, 8.4844) },
            { 8102, (47.4089, 8.4585) },
            { 8103, (47.4046, 8.4461) },
            { 8104, (47.4169, 8.4351) },
            { 8105, (47.4340, 8.4683) },
            { 8106, (47.4397, 8.4572) },

            // Bern
            { 3000, (46.9480, 7.4474) },
            { 3001, (46.9480, 7.4474) },
            { 3003, (46.9416, 7.4507) },
            { 3004, (46.9558, 7.4487) },
            { 3005, (46.9440, 7.4320) },
            { 3006, (46.9388, 7.4614) },
            { 3007, (46.9383, 7.4408) },
            { 3008, (46.9507, 7.4235) },
            { 3010, (46.9480, 7.4474) },
            { 3011, (46.9489, 7.4467) },
            { 3012, (46.9554, 7.4362) },
            { 3013, (46.9587, 7.4573) },
            { 3014, (46.9580, 7.4292) },
            { 3015, (46.9377, 7.4129) },
            { 3018, (46.9604, 7.4087) },
            { 3027, (46.9604, 7.4087) },
            { 3032, (46.9639, 7.3813) },

            // Basel
            { 4000, (47.5596, 7.5886) },
            { 4001, (47.5596, 7.5886) },
            { 4002, (47.5596, 7.5886) },
            { 4051, (47.5540, 7.5892) },
            { 4052, (47.5484, 7.5954) },
            { 4053, (47.5422, 7.5861) },
            { 4054, (47.5487, 7.5727) },
            { 4055, (47.5633, 7.5687) },
            { 4056, (47.5728, 7.5808) },
            { 4057, (47.5696, 7.5994) },
            { 4058, (47.5837, 7.5906) },

            // Geneva
            { 1200, (46.2044, 6.1432) },
            { 1201, (46.2094, 6.1448) },
            { 1202, (46.2197, 6.1430) },
            { 1203, (46.2042, 6.1264) },
            { 1204, (46.1993, 6.1454) },
            { 1205, (46.1946, 6.1386) },
            { 1206, (46.1962, 6.1609) },
            { 1207, (46.2026, 6.1640) },
            { 1208, (46.2070, 6.1706) },
            { 1209, (46.2213, 6.1299) },

            // Lucerne
            { 6000, (47.0502, 8.3093) },
            { 6003, (47.0502, 8.3093) },
            { 6004, (47.0535, 8.3047) },
            { 6005, (47.0470, 8.2948) },
            { 6006, (47.0554, 8.3218) },

            // Lausanne
            { 1000, (46.5197, 6.6323) },
            { 1003, (46.5169, 6.6270) },
            { 1004, (46.5243, 6.6308) },
            { 1005, (46.5267, 6.6387) },
            { 1006, (46.5137, 6.6458) },
            { 1007, (46.5080, 6.6336) },
            { 1010, (46.5316, 6.6262) },
            { 1011, (46.5358, 6.6389) },
            { 1012, (46.5400, 6.6479) },
        };
    }
}
