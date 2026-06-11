// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.Klacksy;

using System.Text.Json;
using Klacks.Api.Application.Klacksy.Models;
using Klacks.Api.Domain.Interfaces.Assistant;

/// <summary>
/// Singleton cache for navigation targets. Loads target metadata from the core manifest (JSON SSOT)
/// and synonyms from the database via INavigationTargetSynonymRepository.
/// TTL 5 min analogous to SkillCacheService. Lookup by targetId or synonym+locale.
/// </summary>
/// <param name="coreManifestPath">Absolute path to the navigation-targets.json file</param>
/// <param name="scopeFactory">Factory for creating DI scopes when querying the scoped synonym repository</param>
public sealed class NavigationTargetCacheService : INavigationTargetCacheService
{
    private sealed record CacheSnapshot(
        List<NavigationTarget> Targets,
        Dictionary<string, NavigationTarget> ById,
        Dictionary<string, Dictionary<string, List<NavigationTarget>>> SynonymIndex,
        DateTime LoadedAt);

    private static readonly CacheSnapshot EmptySnapshot = new([], [], [], DateTime.MinValue);
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

    public IReadOnlyList<NavigationTarget> FindBySynonym(string token, string locale)
    {
        _ = EnsureFreshAsync();
        var snap = _snapshot;
        var normalized = token.Trim().ToLowerInvariant();
        if (!snap.SynonymIndex.TryGetValue(locale, out var byLocale))
        {
            if (locale != "en" && snap.SynonymIndex.TryGetValue("en", out var enFallback))
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

        var filtered = targets.Where(t => !t.Obsolete).ToList();
        _snapshot = new(filtered, filtered.ToDictionary(t => t.TargetId), BuildSynonymIndex(filtered), DateTime.UtcNow);
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
