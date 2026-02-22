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
}
