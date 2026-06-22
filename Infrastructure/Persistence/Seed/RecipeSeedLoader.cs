// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Loads operator-authored recipe definitions from recipe-seeds.json and syncs them with the
/// agent_recipe table. Performs INSERT for new recipes and UPDATE when the seed version exceeds the
/// DB version; a DB row at an equal or higher version is left untouched, so recipes edited directly
/// in the database survive a restart (no redeploy needed to change a recipe).
/// </summary>
using System.Text.Json;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Infrastructure.Persistence.Seed.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Infrastructure.Persistence.Seed;

public class RecipeSeedLoader
{
    private readonly IAgentRecipeRepository _recipeRepository;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<RecipeSeedLoader> _logger;

    private static readonly JsonSerializerOptions JsonReadOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly JsonSerializerOptions JsonWriteOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    private const string SeedFilePath = "Application/Skills/Definitions/recipe-seeds.json";

    public RecipeSeedLoader(
        IAgentRecipeRepository recipeRepository,
        IWebHostEnvironment environment,
        ILogger<RecipeSeedLoader> logger)
    {
        _recipeRepository = recipeRepository;
        _environment = environment;
        _logger = logger;
    }

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        var filePath = Path.Combine(_environment.ContentRootPath, SeedFilePath);

        if (!File.Exists(filePath))
        {
            _logger.LogInformation("Recipe seed file not found at {FilePath}. Skipping.", filePath);
            return;
        }

        RecipeSeedFile seedFile;
        try
        {
            await using var stream = File.OpenRead(filePath);
            seedFile = await JsonSerializer.DeserializeAsync<RecipeSeedFile>(stream, JsonReadOptions, cancellationToken)
                       ?? throw new InvalidDataException("Recipe seed file deserialized to null.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read or deserialize recipe seed file at {FilePath}.", filePath);
            return;
        }

        if (seedFile.Recipes.Count == 0)
        {
            _logger.LogInformation("Recipe seed file contains no recipe definitions. Skipping.");
            return;
        }

        var existing = await _recipeRepository.GetAllAsync(cancellationToken);
        var existingByName = existing.ToDictionary(r => r.Name, StringComparer.OrdinalIgnoreCase);

        var inserted = 0;
        var updated = 0;
        var skipped = 0;

        foreach (var definition in seedFile.Recipes)
        {
            if (string.IsNullOrWhiteSpace(definition.Name))
            {
                _logger.LogWarning("Recipe seed definition has an empty name. Skipping entry.");
                skipped++;
                continue;
            }

            if (existingByName.TryGetValue(definition.Name, out var current))
            {
                if (definition.Version > current.Version)
                {
                    ApplyDefinition(current, definition);
                    await _recipeRepository.UpdateAsync(current, cancellationToken);
                    updated++;
                }
                else
                {
                    skipped++;
                }
            }
            else
            {
                await _recipeRepository.AddAsync(CreateFromDefinition(definition), cancellationToken);
                inserted++;
            }
        }

        _logger.LogInformation(
            "Recipe seed completed: {Total} definitions processed (inserted: {Inserted}, updated: {Updated}, skipped: {Skipped})",
            seedFile.Recipes.Count, inserted, updated, skipped);
    }

    private static AgentRecipe CreateFromDefinition(RecipeSeedDefinition definition)
    {
        return new AgentRecipe
        {
            Name = definition.Name,
            Goal = definition.Goal,
            TriggerJson = JsonSerializer.Serialize(definition.Trigger, JsonWriteOptions),
            StepsJson = JsonSerializer.Serialize(definition.Steps, JsonWriteOptions),
            IsEnabled = definition.IsEnabled,
            SortOrder = definition.SortOrder,
            Version = definition.Version
        };
    }

    private static void ApplyDefinition(AgentRecipe recipe, RecipeSeedDefinition definition)
    {
        recipe.Goal = definition.Goal;
        recipe.TriggerJson = JsonSerializer.Serialize(definition.Trigger, JsonWriteOptions);
        recipe.StepsJson = JsonSerializer.Serialize(definition.Steps, JsonWriteOptions);
        recipe.IsEnabled = definition.IsEnabled;
        recipe.SortOrder = definition.SortOrder;
        recipe.Version = definition.Version;
    }
}
