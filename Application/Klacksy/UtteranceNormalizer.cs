// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.Klacksy;

using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.RegularExpressions;
using Klacks.Api.Application.Klacksy.Models;
using Klacks.Api.Application.Constants;

/// <summary>
/// Normalizes user utterances before matching: lowercases, strips wake-word salutations,
/// maps STT variants, removes filler words. Supports core locales (de/en/fr/it) via
/// wake-word-variants.json and plugin locales via per-locale wake-words.json files.
/// </summary>
public sealed class UtteranceNormalizer : IUtteranceNormalizer
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly WakeWordConfig _config;
    private readonly ConcurrentDictionary<string, (string[] Salutations, string[] FillerWords)> _pluginCache = new();

    public UtteranceNormalizer()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Application", "Klacksy", "wake-word-variants.json");
        var json = File.ReadAllText(path);
        _config = JsonSerializer.Deserialize<WakeWordConfig>(json, JsonOptions)!;
    }

    public NormalizedUtterance Normalize(string raw, string locale)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return new NormalizedUtterance(raw ?? string.Empty, string.Empty, false, true);

        var working = raw.Trim().ToLowerInvariant();

        foreach (var variant in _config.Variants)
            working = Regex.Replace(working, $@"\b{Regex.Escape(variant)}\b", _config.Canonical);

        var coreSalutations = _config.LocaleSalutations.TryGetValue(locale, out var loc) ? loc : Array.Empty<string>();
        var pluginConfig = GetPluginLocaleConfig(locale);
        var allSalutations = _config.UniversalSalutations
            .Concat(coreSalutations)
            .Concat(pluginConfig.Salutations)
            .ToArray();

        var wakePattern = $@"^\s*(?:({string.Join("|", allSalutations.Select(Regex.Escape))})\s+)?{Regex.Escape(_config.Canonical)}\s*[,!\.]?\s*";
        var stripped = Regex.Replace(working, wakePattern, string.Empty);
        var wakeWasStripped = stripped.Length < working.Length;

        var coreFillers = _config.FillerWords.TryGetValue(locale, out var f) ? f : Array.Empty<string>();
        var allFillers = coreFillers.Concat(pluginConfig.FillerWords);
        foreach (var filler in allFillers)
            stripped = Regex.Replace(stripped, $@"\s+{Regex.Escape(filler)}\s*$", string.Empty);

        stripped = stripped.Trim();
        return new NormalizedUtterance(raw, stripped, wakeWasStripped, string.IsNullOrEmpty(stripped));
    }

    private (string[] Salutations, string[] FillerWords) GetPluginLocaleConfig(string locale)
    {
        if (LanguagePluginConstants.CoreLanguages.Contains(locale))
            return (Array.Empty<string>(), Array.Empty<string>());

        return _pluginCache.GetOrAdd(locale, loc =>
        {
            var file = Path.Combine(AppContext.BaseDirectory, LanguagePluginConstants.PluginDirectory, loc, "wake-words.json");
            if (!File.Exists(file))
                return (Array.Empty<string>(), Array.Empty<string>());
            try
            {
                var json = File.ReadAllText(file);
                var data = JsonSerializer.Deserialize<PluginWakeWords>(json, JsonOptions);
                return (data?.Salutations ?? Array.Empty<string>(), data?.FillerWords ?? Array.Empty<string>());
            }
            catch
            {
                return (Array.Empty<string>(), Array.Empty<string>());
            }
        });
    }

    private sealed class WakeWordConfig
    {
        public string Canonical { get; set; } = "klacksy";
        public string[] Variants { get; set; } = Array.Empty<string>();
        public string[] UniversalSalutations { get; set; } = Array.Empty<string>();
        public Dictionary<string, string[]> LocaleSalutations { get; set; } = new();
        public Dictionary<string, string[]> FillerWords { get; set; } = new();
    }

    private sealed class PluginWakeWords
    {
        public string[] Salutations { get; set; } = Array.Empty<string>();
        public string[] FillerWords { get; set; } = Array.Empty<string>();
    }
}
