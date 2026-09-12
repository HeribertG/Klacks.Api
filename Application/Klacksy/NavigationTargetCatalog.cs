// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Adapts the Application-layer INavigationTargetCacheService to the Domain-owned
/// INavigationTargetCatalog contract, so Domain skills (e.g. NavigateToSkill) never
/// need to reference Application types — mirrors the KlacksyPageKeyCatalog pattern.
/// </summary>
/// <param name="cache">Shared navigation target cache (targets + DB-sourced synonyms).</param>
namespace Klacks.Api.Application.Klacksy;

using Klacks.Api.Application.Interfaces.Klacksy;
using Klacks.Api.Application.Klacksy.Models;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

public sealed class NavigationTargetCatalog : INavigationTargetCatalog
{
    private readonly INavigationTargetCacheService _cache;

    public NavigationTargetCatalog(INavigationTargetCacheService cache)
    {
        _cache = cache;
    }

    public IReadOnlyList<NavigationTargetEntry> GetByRoute(string route)
        => _cache.GetByRoute(route).Select(ToEntry).ToList();

    private static NavigationTargetEntry ToEntry(NavigationTarget target)
        => new(
            target.TargetId,
            target.Synonyms.ToDictionary(
                kv => kv.Key,
                kv => (IReadOnlyList<string>)kv.Value),
            target.RequiredPermission,
            target.RequiredFeature);
}
