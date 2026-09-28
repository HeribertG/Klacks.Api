// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Domain-side read access to the navigation target catalog, scoped by route so callers
/// never need to know about the full (147+ entry) global catalog. Implemented in the
/// Application layer (NavigationTargetCatalog) on top of INavigationTargetCacheService,
/// mirroring the IKlacksyPageKeyCatalog dependency-inversion pattern.
/// </summary>
namespace Klacks.Api.Domain.Interfaces.Assistant;

using Klacks.Api.Domain.Models.Assistant;

public interface INavigationTargetCatalog
{
    IReadOnlyList<NavigationTargetEntry> GetByRoute(string route);
}
