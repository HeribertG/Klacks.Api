// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Result of a marketplace region-package lookup. Distinguishes a found package, a marketplace
/// that has no published package for the country (HTTP 404, a normal state) and a failed lookup
/// (network error, server error, invalid payload), so callers can react without treating the
/// not-found case as an error.
/// </summary>
/// <param name="Package">Latest published package info; null unless the lookup found one</param>
/// <param name="NotFound">True when the marketplace answered 404 for the country</param>
namespace Klacks.Api.Application.DTOs.Config;

public sealed record MarketplaceRegionPackageLookup(MarketplaceRegionPackageInfo? Package, bool NotFound)
{
    public static MarketplaceRegionPackageLookup Found(MarketplaceRegionPackageInfo package) => new(package, false);

    public static MarketplaceRegionPackageLookup PackageNotFound() => new(null, true);

    public static MarketplaceRegionPackageLookup Failed() => new(null, false);
}
