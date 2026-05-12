// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.Klacksy;

using System.Text.Json;
using Klacks.Api.Application.Klacksy.Models;

/// <summary>
/// Singleton cache for navigation targets. Loads core manifest + plugin locale overlays at startup.
/// TTL 5 min analogous to SkillCacheService. Lookup by targetId or synonym+locale.
/// </summary>
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
    private readonly string? _pluginFolder;
    private readonly object _lock = new();
    private volatile CacheSnapshot _snapshot = EmptySnapshot;

    public NavigationTargetCacheService(string coreManifestPath, string? pluginFolder)
    {
        _coreManifestPath = coreManifestPath;
        _pluginFolder = pluginFolder;
        Reload();
    }

    public IReadOnlyList<NavigationTarget> All
    {
        get { EnsureFresh(); return _snapshot.Targets; }
    }

    public NavigationTarget? GetById(string targetId)
    {
        EnsureFresh();
        return _snapshot.ById.TryGetValue(targetId, out var t) ? t : null;
    }

    public IReadOnlyList<NavigationTarget> FindBySynonym(string token, string locale)
    {
        EnsureFresh();
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

    public void Invalidate()
    {
        lock (_lock) _snapshot = _snapshot with { LoadedAt = DateTime.MinValue };
    }

    private void EnsureFresh()
    {
        if (DateTime.UtcNow - _snapshot.LoadedAt < Ttl) return;
        lock (_lock)
        {
            if (DateTime.UtcNow - _snapshot.LoadedAt < Ttl) return;
            Reload();
        }
    }

    private void Reload()
    {
        if (!File.Exists(_coreManifestPath))
        {
            _snapshot = EmptySnapshot with { LoadedAt = DateTime.UtcNow };
            return;
        }

        var json = File.ReadAllText(_coreManifestPath);
        var targets = JsonSerializer.Deserialize<List<NavigationTarget>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];

        if (_pluginFolder != null && Directory.Exists(_pluginFolder))
            LoadPluginOverlays(targets);

        var filtered = targets.Where(t => !t.Obsolete).ToList();
        _snapshot = new(filtered, filtered.ToDictionary(t => t.TargetId), BuildSynonymIndex(filtered), DateTime.UtcNow);
    }

    private void LoadPluginOverlays(List<NavigationTarget> targets)
    {
        foreach (var dir in Directory.EnumerateDirectories(_pluginFolder!))
        {
            var locale = Path.GetFileName(dir);
            var file = Path.Combine(dir, "navigation-targets.json");
            if (!File.Exists(file)) continue;
            var overlay = JsonSerializer.Deserialize<Dictionary<string, PluginOverlayEntry>>(File.ReadAllText(file)) ?? new();
            foreach (var (targetId, entry) in overlay)
            {
                var t = targets.FirstOrDefault(x => x.TargetId == targetId);
                if (t == null) continue;
                t.Synonyms[locale] = entry.Synonyms;
            }
        }
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

    private sealed class PluginOverlayEntry
    {
        public string[] Synonyms { get; set; } = Array.Empty<string>();
        public string Status { get; set; } = "generated";
    }
}
