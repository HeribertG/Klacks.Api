// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.Klacksy;

using System.Text.Json;
using System.Text.RegularExpressions;
using Klacks.Api.Application.Klacksy.Models;

/// <summary>
/// Normalizes user utterances before matching: lowercases, strips wake-word salutations,
/// maps STT variants, removes filler words.
/// </summary>
public sealed class UtteranceNormalizer : IUtteranceNormalizer
{
    private readonly WakeWordConfig _config;

    public UtteranceNormalizer()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Application", "Klacksy", "wake-word-variants.json");
        var json = File.ReadAllText(path);
        _config = JsonSerializer.Deserialize<WakeWordConfig>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
    }

    public NormalizedUtterance Normalize(string raw, string locale)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return new NormalizedUtterance(raw ?? string.Empty, string.Empty, false, true);

        var working = raw.Trim().ToLowerInvariant();

        foreach (var variant in _config.Variants)
            working = Regex.Replace(working, $@"\b{Regex.Escape(variant)}\b", _config.Canonical);

        var salutations = _config.LocaleSalutations.TryGetValue(locale, out var loc) ? loc : Array.Empty<string>();
        var allSalutations = _config.UniversalSalutations.Concat(salutations).ToArray();

        var wakePattern = $@"^\s*(?:({string.Join("|", allSalutations.Select(Regex.Escape))})\s+)?{Regex.Escape(_config.Canonical)}\s*[,!\.]?\s*";
        var stripped = Regex.Replace(working, wakePattern, string.Empty);
        var wakeWasStripped = stripped.Length < working.Length;

        var fillers = _config.FillerWords.TryGetValue(locale, out var f) ? f : Array.Empty<string>();
        foreach (var filler in fillers)
            stripped = Regex.Replace(stripped, $@"\s+{Regex.Escape(filler)}\s*$", string.Empty);

        stripped = stripped.Trim();
        return new NormalizedUtterance(raw, stripped, wakeWasStripped, string.IsNullOrEmpty(stripped));
    }

    private sealed class WakeWordConfig
    {
        public string Canonical { get; set; } = "klacksy";
        public string[] Variants { get; set; } = Array.Empty<string>();
        public string[] UniversalSalutations { get; set; } = Array.Empty<string>();
        public Dictionary<string, string[]> LocaleSalutations { get; set; } = new();
        public Dictionary<string, string[]> FillerWords { get; set; } = new();
    }
}
