// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.Klacksy;

using Klacks.Api.Application.Interfaces.Klacksy;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.RegularExpressions;
using Klacks.Api.Application.Klacksy.Models;
using Klacks.Api.Application.Constants;

/// <summary>
/// Normalizes user utterances before matching: lowercases, strips wake-word salutations,
/// maps STT variants, strips a leading command prefix ("show me", "zeig mir mal") and a
/// leading article, then removes filler words at the end. Prefix and article stripping only
/// ever run on the core locales (de/en/fr/it) declared in wake-word-variants.json; any other
/// locale has no configured list and is left untouched. Plugin locales additionally get their
/// own salutations/filler words from per-locale wake-words.json files, and languages written
/// without spaces (ja, zh, th) get command prefixes/suffixes that are stripped without a word
/// boundary. The wake word may follow a salutation without a space and be followed by CJK
/// punctuation ("你好klacksy，…", "klacksy、…").
/// </summary>
public sealed class UtteranceNormalizer : IUtteranceNormalizer
{
    private const int MaxPrefixStripIterations = 5;

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly WakeWordConfig _config;
    private readonly IReadOnlyDictionary<string, string[]> _prefixesByLocale;
    private readonly IReadOnlyDictionary<string, string[]> _articlesByLocale;
    private readonly ConcurrentDictionary<string, PluginLocaleConfig> _pluginCache = new();

    public UtteranceNormalizer()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Application", "Klacksy", "wake-word-variants.json");
        var json = File.ReadAllText(path);
        _config = JsonSerializer.Deserialize<WakeWordConfig>(json, JsonOptions)!;
        _prefixesByLocale = SortByDescendingLength(_config.Prefixes);
        _articlesByLocale = SortByDescendingLength(_config.Articles);
    }

    private static IReadOnlyDictionary<string, string[]> SortByDescendingLength(Dictionary<string, string[]> byLocale)
        => byLocale.ToDictionary(
            kv => kv.Key,
            kv => kv.Value.OrderByDescending(phrase => phrase.Length).ToArray());

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

        var wakePattern = $@"^\s*(?:({string.Join("|", allSalutations.Select(Regex.Escape))})\s*)?{Regex.Escape(_config.Canonical)}\s*[,!\.、，！。:：]?\s*";
        var stripped = Regex.Replace(working, wakePattern, string.Empty);
        var wakeWasStripped = stripped.Length < working.Length;

        var beforePrefixPhase = stripped;
        var prefixes = _prefixesByLocale.TryGetValue(locale, out var pre) ? pre : Array.Empty<string>();
        var articles = _articlesByLocale.TryGetValue(locale, out var art) ? art : Array.Empty<string>();
        stripped = StripLeadingPhrases(stripped, prefixes);
        stripped = StripLeadingPhrases(stripped, articles);
        stripped = StripUnspacedPrefixes(stripped, pluginConfig.CommandPrefixes);
        if (string.IsNullOrWhiteSpace(stripped))
            stripped = beforePrefixPhase;

        var coreFillers = _config.FillerWords.TryGetValue(locale, out var f) ? f : Array.Empty<string>();
        var allFillers = coreFillers.Concat(pluginConfig.FillerWords);
        foreach (var filler in allFillers)
            stripped = Regex.Replace(stripped, $@"\s+{Regex.Escape(filler)}\s*$", string.Empty);

        var beforeSuffixPhase = stripped;
        stripped = StripUnspacedSuffixes(stripped, pluginConfig.CommandSuffixes);
        if (string.IsNullOrWhiteSpace(stripped))
            stripped = beforeSuffixPhase;

        stripped = stripped.Trim();
        return new NormalizedUtterance(raw, stripped, wakeWasStripped, string.IsNullOrEmpty(stripped));
    }

    private static string StripLeadingPhrases(string input, string[] phrasesLongestFirst)
    {
        if (phrasesLongestFirst.Length == 0)
            return input;

        var pattern = $@"^\s*(?:{string.Join("|", phrasesLongestFirst.Select(Regex.Escape))})\b\s*";
        var current = input;
        for (var i = 0; i < MaxPrefixStripIterations; i++)
        {
            var next = Regex.Replace(current, pattern, string.Empty);
            if (next == current)
                break;
            current = next;
        }

        return current;
    }

    // Languages written without spaces between words (ja, zh, th) put the command right against the
    // noun: "残業を見せて", "打开设置". The spaced prefix and filler rules never fire there, so these
    // plugin-supplied phrases are stripped without any word boundary, longest first, and only at the
    // very start or end of the utterance.
    private static string StripUnspacedPrefixes(string input, string[] phrasesLongestFirst)
    {
        var current = input.TrimStart();
        for (var i = 0; i < MaxPrefixStripIterations; i++)
        {
            var hit = phrasesLongestFirst.FirstOrDefault(p => current.StartsWith(p, StringComparison.Ordinal));
            if (hit == null)
                break;
            current = current[hit.Length..].TrimStart();
        }

        return current;
    }

    private static string StripUnspacedSuffixes(string input, string[] phrasesLongestFirst)
    {
        var current = input.TrimEnd();
        for (var i = 0; i < MaxPrefixStripIterations; i++)
        {
            var hit = phrasesLongestFirst.FirstOrDefault(p => current.EndsWith(p, StringComparison.Ordinal));
            if (hit == null)
                break;
            current = current[..^hit.Length].TrimEnd();
        }

        return current;
    }

    private PluginLocaleConfig GetPluginLocaleConfig(string locale)
    {
        if (LanguagePluginConstants.CoreLanguages.Contains(locale))
            return PluginLocaleConfig.Empty;

        return _pluginCache.GetOrAdd(locale, loc =>
        {
            var file = Path.Combine(AppContext.BaseDirectory, LanguagePluginConstants.PluginDirectory, loc, "wake-words.json");
            if (!File.Exists(file))
                return PluginLocaleConfig.Empty;
            try
            {
                var json = File.ReadAllText(file);
                var data = JsonSerializer.Deserialize<PluginWakeWords>(json, JsonOptions);
                if (data == null)
                    return PluginLocaleConfig.Empty;
                return new PluginLocaleConfig(
                    data.Salutations,
                    data.FillerWords,
                    data.CommandPrefixes.OrderByDescending(p => p.Length).ToArray(),
                    data.CommandSuffixes.OrderByDescending(p => p.Length).ToArray());
            }
            catch
            {
                return PluginLocaleConfig.Empty;
            }
        });
    }

    private sealed record PluginLocaleConfig(string[] Salutations, string[] FillerWords, string[] CommandPrefixes, string[] CommandSuffixes)
    {
        public static readonly PluginLocaleConfig Empty = new([], [], [], []);
    }

    private sealed class WakeWordConfig
    {
        public string Canonical { get; set; } = "klacksy";
        public string[] Variants { get; set; } = Array.Empty<string>();
        public string[] UniversalSalutations { get; set; } = Array.Empty<string>();
        public Dictionary<string, string[]> LocaleSalutations { get; set; } = new();
        public Dictionary<string, string[]> FillerWords { get; set; } = new();
        public Dictionary<string, string[]> Prefixes { get; set; } = new();
        public Dictionary<string, string[]> Articles { get; set; } = new();
    }

    private sealed class PluginWakeWords
    {
        public string[] Salutations { get; set; } = Array.Empty<string>();
        public string[] FillerWords { get; set; } = Array.Empty<string>();
        public string[] CommandPrefixes { get; set; } = Array.Empty<string>();
        public string[] CommandSuffixes { get; set; } = Array.Empty<string>();
    }
}
