// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Seeds navigation target synonyms from the core navigation-targets.json manifest into the database.
/// Reconciles vendor-owned (Source == "seed") rows against the manifest row by row per (TargetId,
/// Language) pair, never touching customer-trained ("user") or plugin-installed ("plugin") rows in the
/// same pair.
/// </summary>
/// <param name="repository">Repository for navigation target synonym persistence</param>
/// <param name="environment">Provides the content root path for locating the manifest file</param>
/// <param name="logger">Logger for diagnostic output</param>
using System.Text.Json;
using Klacks.Api.Application.Klacksy.Models;
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

        var insertedRows = 0;
        var removedRows = 0;
        var unchangedPairs = 0;
        var untouchedForeignRows = 0;

        foreach (var target in targets)
        {
            foreach (var (language, keywords) in target.Synonyms)
            {
                if (keywords.Length == 0)
                    continue;

                var result = await _repository.SyncSeedKeywordsForTargetLanguageAsync(target.TargetId, language, keywords, ct);

                if (result.InsertedCount == 0 && result.RemovedCount == 0)
                {
                    unchangedPairs++;
                }
                else
                {
                    insertedRows += result.InsertedCount;
                    removedRows += result.RemovedCount;
                }

                untouchedForeignRows += result.UntouchedForeignCount;
            }
        }

        _logger.LogInformation(
            "Navigation target synonym seed completed: {InsertedRows} seed rows inserted, {RemovedRows} seed rows removed, {UnchangedPairs} pairs unchanged, {UntouchedForeignRows} customer/plugin-owned rows left untouched.",
            insertedRows, removedRows, unchangedPairs, untouchedForeignRows);
    }
}
