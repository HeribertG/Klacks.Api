// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.Klacksy;

using Klacks.Api.Application.Interfaces.Klacksy;
using System.Text.Json;
using Klacks.Api.Application.Klacksy.Models;
using Klacks.Api.Domain.Interfaces.Assistant;

/// <summary>
/// Singleton cache for navigation targets. Loads target metadata from the core manifest (JSON SSOT)
/// and synonyms from the database via INavigationTargetSynonymRepository.
/// TTL 5 min analogous to SkillCacheService. Lookup by targetId or synonym+locale.
/// Lookups refresh fire-and-forget and serve the current snapshot, so WarmUpAsync must be awaited at
/// startup — otherwise the first request after a restart sees an empty snapshot.
/// Obsolete targets and targets of a feature this installation does not have are dropped while the
/// snapshot is built, so the matcher never offers a destination the router would bounce.
/// </summary>
/// <param name="coreManifestPath">Absolute path to the navigation-targets.json file</param>
/// <param name="scopeFactory">Factory for creating DI scopes when querying the scoped synonym repository</param>
public sealed class NavigationTargetCacheService : INavigationTargetCacheService
{
    private sealed record CacheSnapshot(
        List<NavigationTarget> Targets,
        Dictionary<string, NavigationTarget> ById,
        Dictionary<string, List<NavigationTarget>> ByRoute,
        Dictionary<string, Dictionary<string, List<NavigationTarget>>> SynonymIndex,
        DateTime LoadedAt);

    private static readonly CacheSnapshot EmptySnapshot = new([], [], [], [], DateTime.MinValue);
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(5);

    private readonly string _coreManifestPath;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private volatile CacheSnapshot _snapshot = EmptySnapshot;

    public NavigationTargetCacheService(string coreManifestPath, IServiceScopeFactory scopeFactory)
    {
        _coreManifestPath = coreManifestPath;
        _scopeFactory = scopeFactory;
    }

    public IReadOnlyList<NavigationTarget> All
    {
        get { _ = EnsureFreshAsync(); return _snapshot.Targets; }
    }

    public NavigationTarget? GetById(string targetId)
    {
        _ = EnsureFreshAsync();
        return _snapshot.ById.TryGetValue(targetId, out var t) ? t : null;
    }

    public IReadOnlyList<NavigationTarget> GetByRoute(string route)
    {
        _ = EnsureFreshAsync();
        return _snapshot.ByRoute.TryGetValue(route, out var list) ? list : Array.Empty<NavigationTarget>();
    }

    public IReadOnlyList<NavigationTarget> FindBySynonym(string token, string locale)
    {
        _ = EnsureFreshAsync();
        var snap = _snapshot;
        var normalized = token.Trim().ToLowerInvariant();
        if (!snap.SynonymIndex.TryGetValue(locale, out var byLocale))
        {
            if (locale != NavigationLocaleConstants.English && snap.SynonymIndex.TryGetValue(NavigationLocaleConstants.English, out var enFallback))
                byLocale = enFallback;
            else
                return Array.Empty<NavigationTarget>();
        }

        return byLocale.TryGetValue(normalized, out var list) ? list : Array.Empty<NavigationTarget>();
    }

    public IReadOnlyList<NavigationTarget> FindBySynonymAnyLocale(string token)
    {
        _ = EnsureFreshAsync();
        var snap = _snapshot;
        var normalized = token.Trim().ToLowerInvariant();
        var combined = new List<NavigationTarget>();
        var seen = new HashSet<string>();
        foreach (var byLocale in snap.SynonymIndex.Values)
        {
            if (!byLocale.TryGetValue(normalized, out var list))
                continue;
            foreach (var target in list)
            {
                if (seen.Add(target.TargetId))
                    combined.Add(target);
            }
        }

        return combined;
    }

    public void Invalidate()
    {
        _snapshot = _snapshot with { LoadedAt = DateTime.MinValue };
    }

    public async Task WarmUpAsync(CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            await ReloadAsync();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task EnsureFreshAsync()
    {
        if (DateTime.UtcNow - _snapshot.LoadedAt < Ttl) return;
        await _semaphore.WaitAsync();
        try
        {
            if (DateTime.UtcNow - _snapshot.LoadedAt < Ttl) return;
            await ReloadAsync();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task ReloadAsync()
    {
        if (!File.Exists(_coreManifestPath))
        {
            _snapshot = EmptySnapshot with { LoadedAt = DateTime.UtcNow };
            return;
        }

        var json = File.ReadAllText(_coreManifestPath);
        var targets = JsonSerializer.Deserialize<List<NavigationTarget>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];

        using var scope = _scopeFactory.CreateScope();
        var synonymRepo = scope.ServiceProvider.GetRequiredService<INavigationTargetSynonymRepository>();
        var allSynonyms = await synonymRepo.GetAllAsync();
        var unavailableFeatures = await ResolveUnavailableFeaturesAsync(scope, targets);

        foreach (var target in targets)
        {
            target.Synonyms = new Dictionary<string, string[]>();
        }

        var byTargetId = targets.ToDictionary(t => t.TargetId);

        foreach (var group in allSynonyms.GroupBy(s => new { s.TargetId, s.Language }))
        {
            if (!byTargetId.TryGetValue(group.Key.TargetId, out var target))
                continue;

            target.Synonyms[group.Key.Language] = group.Select(s => s.Keyword).ToArray();
        }

        var filtered = targets
            .Where(t => !t.Obsolete)
            .Where(t => t.RequiredFeature == null || !unavailableFeatures.Contains(t.RequiredFeature))
            .ToList();
        _snapshot = new(
            filtered,
            filtered.ToDictionary(t => t.TargetId),
            filtered.GroupBy(t => t.Route).ToDictionary(g => g.Key, g => g.ToList()),
            BuildSynonymIndex(filtered),
            DateTime.UtcNow);
    }

    /// <summary>
    /// Feature-gated destinations leave the snapshot exactly like obsolete ones: the matcher is the chat
    /// fast-path and does not ask a second time, so a target kept here on an installation without its
    /// feature is a navigation the router then bounces to /no-access. Each distinct feature is asked
    /// about once per reload. The answer is only as fresh as the snapshot - the TTL bounds the staleness,
    /// and the plugin lifecycle and the incoming-server settings write call Invalidate() to shorten it.
    /// </summary>
    /// <param name="scope">The reload scope, also used for the synonym repository</param>
    /// <param name="targets">All targets read from the manifest, before filtering</param>
    private static async Task<HashSet<string>> ResolveUnavailableFeaturesAsync(
        IServiceScope scope, List<NavigationTarget> targets)
    {
        var features = targets
            .Select(t => t.RequiredFeature)
            .Where(f => !string.IsNullOrWhiteSpace(f))
            .Select(f => f!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var unavailable = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (features.Count == 0)
        {
            return unavailable;
        }

        var availability = scope.ServiceProvider.GetRequiredService<IFeatureAvailabilityService>();
        foreach (var feature in features)
        {
            if (!await availability.IsAvailableAsync(feature))
            {
                unavailable.Add(feature);
            }
        }

        return unavailable;
    }

    private static Dictionary<string, Dictionary<string, List<NavigationTarget>>> BuildSynonymIndex(List<NavigationTarget> targets)
    {
        var index = new Dictionary<string, Dictionary<string, List<NavigationTarget>>>();
        foreach (var t in targets)
        {
            foreach (var (locale, synonyms) in t.Synonyms)
            {
                if (!index.TryGetValue(locale, out var byLocale)) index[locale] = byLocale = new();
                foreach (var syn in synonyms)
                {
                    var key = syn.ToLowerInvariant();
                    if (!byLocale.TryGetValue(key, out var list)) byLocale[key] = list = new();
                    list.Add(t);
                }
            }
        }

        return index;
    }
}
