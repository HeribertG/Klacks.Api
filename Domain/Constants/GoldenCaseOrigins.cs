// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Where a golden case came from. A cluster-born case is evidence one user produced; a goldset-born case
/// is a curated expectation shipped with the product. They are kept apart because only the goldset ones
/// can be partitioned into a training half the gate never sees.
/// </summary>
namespace Klacks.Api.Domain.Constants;

public static class GoldenCaseOrigins
{
    public const string Cluster = "cluster";
    public const string Goldset = "goldset";
}
