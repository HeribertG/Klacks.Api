// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Builds transcription dictionary context from DB-stored entries.
/// Uses IMemoryCache with 5 minute TTL to avoid rebuilding on every request.
/// </summary>
/// <param name="repository">Repository for transcription dictionary entries</param>
/// <param name="cache">In-memory cache for dictionary context</param>
/// <param name="logger">Logger instance</param>
namespace Klacks.Api.Infrastructure.Services.Assistant;

using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

public class DictionaryService : IDictionaryService
{
    private readonly ITranscriptionDictionaryRepository _repository;
    private readonly IMemoryCache _cache;
    private readonly ILogger<DictionaryService> _logger;
    private const string CacheKey = "TranscriptionDictionary";
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
        if (_cache.TryGetValue(CacheKey, out string? cached) && cached != null)
            return cached;

        var entries = await _repository.GetAllAsync(ct);
        var context = BuildContextString(entries);

        _cache.Set(CacheKey, context, CacheDuration);
        _logger.LogDebug("Built transcription dictionary context with {Count} entries", entries.Count);

        return context;
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
}
