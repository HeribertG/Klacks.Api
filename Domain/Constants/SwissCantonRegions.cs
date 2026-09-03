// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Constants;

/// <summary>
/// Mirrors the deterministic canton-to-region assignment baked into
/// Infrastructure/Persistence/Seed/GroupsSeed.cs (GenerateInsertScriptForGroups), so a skill that builds
/// a canton/city group hierarchy from scratch nests it exactly the way the demo seed would. All 26 Swiss
/// cantons are covered; a code outside this map has no seeded region and falls back to the root level.
/// </summary>
public static class SwissCantonRegions
{
    public const string Westschweiz = "Westschweiz";
    public const string DeutschschweizZuerich = "Deutschschweiz Zürich";
    public const string DeutschschweizMitte = "Deutschschweiz Mitte";
    public const string DeutschschweizOst = "Deutschschweiz Ost";

    public static readonly IReadOnlyDictionary<string, string> ByCantonCode =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["GE"] = Westschweiz,
            ["VD"] = Westschweiz,
            ["NE"] = Westschweiz,
            ["JU"] = Westschweiz,
            ["FR"] = Westschweiz,
            ["ZH"] = DeutschschweizZuerich,
            ["AG"] = DeutschschweizZuerich,
            ["BE"] = DeutschschweizMitte,
            ["SO"] = DeutschschweizMitte,
            ["BS"] = DeutschschweizMitte,
            ["BL"] = DeutschschweizMitte,
            ["LU"] = DeutschschweizOst,
            ["SG"] = DeutschschweizOst,
            ["TG"] = DeutschschweizOst,
            ["AI"] = DeutschschweizOst,
            ["AR"] = DeutschschweizOst,
            ["GL"] = DeutschschweizOst,
            ["GR"] = DeutschschweizOst,
            ["NW"] = DeutschschweizOst,
            ["OW"] = DeutschschweizOst,
            ["SH"] = DeutschschweizOst,
            ["SZ"] = DeutschschweizOst,
            ["TI"] = DeutschschweizOst,
            ["UR"] = DeutschschweizOst,
            ["VS"] = DeutschschweizOst,
            ["ZG"] = DeutschschweizOst,
        };
}
