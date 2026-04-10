// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Builds transcription dictionary context from static Klacks-specific entries.
/// Uses IMemoryCache with 5 minute TTL to avoid rebuilding on every request.
/// </summary>
/// <param name="cache">In-memory cache for dictionary context</param>
/// <param name="logger">Logger instance</param>
namespace Klacks.Api.Infrastructure.Services.Assistant;

using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

public class DictionaryService : IDictionaryService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<DictionaryService> _logger;
    private const string CacheKey = "TranscriptionDictionary";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public DictionaryService(IMemoryCache cache, ILogger<DictionaryService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<string> BuildContextAsync(CancellationToken ct = default)
    {
        if (_cache.TryGetValue(CacheKey, out string? cached) && cached != null)
            return cached;

        var entries = GetStaticEntries();
        var context = BuildContextString(entries);

        _cache.Set(CacheKey, context, CacheDuration);
        _logger.LogDebug("Built transcription dictionary context with {Count} entries", entries.Count);

        return await Task.FromResult(context);
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

    private static List<TranscriptionDictionaryEntry> GetStaticEntries()
    {
        return
        [
            new() { CorrectTerm = "Klacks", Category = "app", PhoneticVariants = ["Klax", "Klags", "Clacks"] },
            new() { CorrectTerm = "Klacksy", Category = "app", PhoneticVariants = ["Klaksi", "Clacksy", "Klaksy"] },
            new() { CorrectTerm = "Dienstplan", Category = "app", PhoneticVariants = ["Dienst Plan"] },
            new() { CorrectTerm = "Schichtplan", Category = "app", PhoneticVariants = ["Schicht Plan"] },
            new() { CorrectTerm = "FD", Category = "shift", Description = "Frühdienst" },
            new() { CorrectTerm = "SD", Category = "shift", Description = "Spätdienst" },
            new() { CorrectTerm = "ND", Category = "shift", Description = "Nachtdienst" },
            new() { CorrectTerm = "BD", Category = "shift", Description = "Bereitschaftsdienst" },
            new() { CorrectTerm = "Frühdienst", Category = "shift", PhoneticVariants = ["Früh Dienst"] },
            new() { CorrectTerm = "Spätdienst", Category = "shift", PhoneticVariants = ["Spät Dienst"] },
            new() { CorrectTerm = "Nachtdienst", Category = "shift", PhoneticVariants = ["Nacht Dienst"] },
            new() { CorrectTerm = "Überstunden", Category = "hr" },
            new() { CorrectTerm = "Zuschläge", Category = "hr" },
            new() { CorrectTerm = "Urlaubskonto", Category = "hr", PhoneticVariants = ["Urlaubs Konto"] },
            new() { CorrectTerm = "Resturlaub", Category = "hr", PhoneticVariants = ["Rest Urlaub"] },
            new() { CorrectTerm = "Planperiode", Category = "hr", PhoneticVariants = ["Plan Periode"] },
            new() { CorrectTerm = "Besetzung", Category = "hr" },
        ];
    }
}
