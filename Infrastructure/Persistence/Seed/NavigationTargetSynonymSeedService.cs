// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Seeds navigation target synonyms from the core navigation-targets.json manifest into the database.
/// Source-guarded upsert: inserts when no entries exist, refreshes only vendor-owned (Source == "seed")
/// pairs when the keyword set changed, and never overwrites customer-trained or plugin-installed pairs.
/// </summary>
/// <param name="repository">Repository for navigation target synonym persistence</param>
/// <param name="environment">Provides the content root path for locating the manifest file</param>
/// <param name="logger">Logger for diagnostic output</param>
using System.Text.Json;
using Klacks.Api.Application.Klacksy.Models;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Infrastructure.Persistence.Seed;

public class NavigationTargetSynonymSeedService
{
    private const string ManifestRelativePath = "Application/Skills/Definitions/navigation-targets.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly INavigationTargetSynonymRepository _repository;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<NavigationTargetSynonymSeedService> _logger;

    public NavigationTargetSynonymSeedService(
        INavigationTargetSynonymRepository repository,
        IWebHostEnvironment environment,
        ILogger<NavigationTargetSynonymSeedService> logger)
    {
        _repository = repository;
        _environment = environment;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        var filePath = Path.Combine(_environment.ContentRootPath, ManifestRelativePath);
        if (!File.Exists(filePath))
        {
            _logger.LogInformation("Navigation targets manifest not found at {Path}. Skipping synonym seed.", filePath);
            return;
        }

        List<NavigationTarget> targets;
        try
        {
            await using var stream = File.OpenRead(filePath);
            targets = await JsonSerializer.DeserializeAsync<List<NavigationTarget>>(stream, JsonOptions, ct)
                      ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deserialize navigation targets manifest at {Path}.", filePath);
            return;
        }

        var inserted = 0;
        var refreshed = 0;
        var skipped = 0;
        var protectedCount = 0;

        foreach (var target in targets)
        {
            foreach (var (language, keywords) in target.Synonyms)
            {
                if (keywords.Length == 0)
                    continue;

                var existing = await _repository.GetActiveForTargetLanguageAsync(target.TargetId, language, ct);

                if (existing.Count == 0)
                {
                    await _repository.ReplaceForTargetLanguageAsync(target.TargetId, language, keywords, SynonymSources.Seed, ct);
                    inserted++;
                    continue;
                }

                if (!existing.All(e => string.Equals(e.Source, SynonymSources.Seed, StringComparison.OrdinalIgnoreCase)))
                {
                    protectedCount++;
                    continue;
                }

                var existingKeywords = existing.Select(e => e.Keyword).ToHashSet(StringComparer.Ordinal);
                if (existingKeywords.SetEquals(keywords))
                {
                    skipped++;
                    continue;
                }

                await _repository.ReplaceForTargetLanguageAsync(target.TargetId, language, keywords, SynonymSources.Seed, ct);
                refreshed++;
            }
        }

        _logger.LogInformation(
            "Navigation target synonym seed completed: {Inserted} inserted, {Refreshed} refreshed, {Skipped} unchanged, {Protected} protected (customer/plugin-owned).",
            inserted, refreshed, skipped, protectedCount);
    }
}
