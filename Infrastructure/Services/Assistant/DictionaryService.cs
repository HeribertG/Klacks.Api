// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Builds transcription dictionary context from DB-stored entries and applies deterministic
/// phonetic-variant replacements. Uses IMemoryCache with 5 minute TTL to avoid hitting the DB
/// on every request.
/// </summary>
/// <param name="repository">Repository for transcription dictionary entries</param>
/// <param name="cache">In-memory cache for dictionary entries</param>
/// <param name="logger">Logger instance</param>
namespace Klacks.Api.Infrastructure.Services.Assistant;

using System.Text.RegularExpressions;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

public class DictionaryService : IDictionaryService
{
    private readonly ITranscriptionDictionaryRepository _repository;
    private readonly IMemoryCache _cache;
    private readonly ILogger<DictionaryService> _logger;
    private const string EntriesCacheKey = "TranscriptionDictionary.Entries";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public DictionaryService(
        ITranscriptionDictionaryRepository repository,
        IMemoryCache cache,
        ILogger<DictionaryService> logger)
    {
        _repository = repository;
        _cache = cache;
        _logger = logger;
    }

    public async Task<string> BuildContextAsync(CancellationToken ct = default)
    {
        var entries = await GetCachedEntriesAsync(ct);
        return BuildContextString(entries);
    }

    public async Task<string> ApplyReplacementsAsync(string text, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        var entries = await GetCachedEntriesAsync(ct);
        return ApplyDeterministicReplacements(text, entries);
    }

    public void InvalidateCache()
    {
        _cache.Remove(EntriesCacheKey);
    }

    private async Task<List<TranscriptionDictionaryEntry>> GetCachedEntriesAsync(CancellationToken ct)
    {
        if (_cache.TryGetValue(EntriesCacheKey, out List<TranscriptionDictionaryEntry>? cached) && cached != null)
        {
            return cached;
        }

        var entries = await _repository.GetAllAsync(ct);
        _cache.Set(EntriesCacheKey, entries, CacheDuration);
        _logger.LogDebug("Loaded transcription dictionary into cache with {Count} entries", entries.Count);
        return entries;
    }

    private static string BuildContextString(List<TranscriptionDictionaryEntry> entries)
    {
        var lines = entries.Select(e =>
        {
            var variants = e.PhoneticVariants.Count > 0
                ? string.Join(", ", e.PhoneticVariants.Select(v => $"\"{v}\"")) + " → "
                : "";
            var desc = !string.IsNullOrEmpty(e.Description) ? $" ({e.Description})" : "";
            return $"- {variants}{e.CorrectTerm}{desc}";
        });

        return string.Join("\n", lines);
    }

    private static string ApplyDeterministicReplacements(string text, List<TranscriptionDictionaryEntry> entries)
    {
        var pairs = entries
            .SelectMany(e => e.PhoneticVariants
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Select(v => new { Variant = v, Correct = e.CorrectTerm }))
            .OrderByDescending(p => p.Variant.Length)
            .ToList();

        if (pairs.Count == 0)
        {
            return text;
        }

        var result = text;
        foreach (var pair in pairs)
        {
            var pattern = $@"(?<!\w){Regex.Escape(pair.Variant)}(?!\w)";
            result = Regex.Replace(result, pattern, pair.Correct, RegexOptions.IgnoreCase);
        }
        return result;
    }
}
