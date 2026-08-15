// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Constants;

public static class RegexDefaults
{
    // The .NET default is 15 entries. The seeded recipes alone contribute 23 distinct anyWordStart
    // patterns (recipe-seeds.json, measured 2026-08-09), and recipes are editable at runtime, so the
    // ceiling is generous enough that adding recipes does not silently reintroduce cache thrashing.
    public const int CacheSize = 64;
}
