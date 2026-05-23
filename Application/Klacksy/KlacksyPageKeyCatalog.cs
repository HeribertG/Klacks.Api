// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Singleton catalog of Klacksy navigation page-keys. Loads the auto-generated
/// klacksy-page-keys.generated.json (written by Klacks.Ui/tools/scan-klacksy-targets.ts)
/// once at construction and serves lookups in O(1) by page-key.
/// </summary>
namespace Klacks.Api.Application.Klacksy;

using System.Text.Json;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

public sealed class KlacksyPageKeyCatalog : IKlacksyPageKeyCatalog
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly IReadOnlyList<KlacksyPageKeyEntry> _entries;
    private readonly IReadOnlyDictionary<string, KlacksyPageKeyEntry> _byPageKey;

    public KlacksyPageKeyCatalog(string manifestPath)
    {
        _entries = LoadEntries(manifestPath);
        _byPageKey = _entries.ToDictionary(
            e => e.PageKey,
            e => e,
            StringComparer.OrdinalIgnoreCase);
    }

    public KlacksyPageKeyEntry? GetByPageKey(string pageKey)
        => _byPageKey.TryGetValue(pageKey, out var entry) ? entry : null;

    public IReadOnlyList<string> AllPageKeys => _entries.Select(e => e.PageKey).ToList();

    public IReadOnlyList<KlacksyPageKeyEntry> All => _entries;

    private static IReadOnlyList<KlacksyPageKeyEntry> LoadEntries(string manifestPath)
    {
        if (!File.Exists(manifestPath))
            return Array.Empty<KlacksyPageKeyEntry>();

        var raw = File.ReadAllText(manifestPath);
        var doc = JsonSerializer.Deserialize<ManifestFile>(raw, JsonOptions);
        return doc?.Entries.AsReadOnly() ?? (IReadOnlyList<KlacksyPageKeyEntry>)Array.Empty<KlacksyPageKeyEntry>();
    }

    private sealed class ManifestFile
    {
        public string? GeneratedAt { get; set; }
        public string? Source { get; set; }
        public List<KlacksyPageKeyEntry> Entries { get; set; } = new();
    }
}
