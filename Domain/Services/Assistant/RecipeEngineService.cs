// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Resolves and resumes data-driven recipes for the chat loop. ResolveAsync matches the message
/// against the enabled recipes (loaded from the database) and builds a fresh execution plan; ResumeAsync
/// rebuilds the plan paused on an ask step from the pending store. Persist/Clear manage the durable slot
/// bag across ask turns. The recipe definitions live in the database, so new recipes are added without a
/// recompile (seeded from recipe-seeds.json, editable directly in the table).
///
/// Database reads run in their OWN service scope (not the request-scoped DataBaseContext): the chat
/// pipeline launches fire-and-forget tasks (e.g. MemoryRetrievalService updating access counts) that
/// touch the request context concurrently, so a read on the shared context here races with them. A
/// fresh scope per lookup is fully isolated; recipes are startup-seeded, so a fresher read is correct.
/// </summary>

using System.Text.Json;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Assistant.Recipes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Domain.Services.Assistant;

public class RecipeEngineService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IPendingRecipeStore _pendingRecipeStore;
    private readonly ILogger<RecipeEngineService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public RecipeEngineService(
        IServiceScopeFactory scopeFactory,
        IPendingRecipeStore pendingRecipeStore,
        ILogger<RecipeEngineService> logger)
    {
        _scopeFactory = scopeFactory;
        _pendingRecipeStore = pendingRecipeStore;
        _logger = logger;
    }

    public async Task<RecipeExecutionPlan?> ResolveAsync(
        string? message, string? language = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return null;
        }

        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IAgentRecipeRepository>();
        var recipes = await repository.GetAllEnabledAsync(cancellationToken);

        foreach (var recipe in recipes)
        {
            var trigger = Deserialize<RecipeTrigger>(recipe.TriggerJson);
            if (trigger == null || !RecipeTriggerMatcher.Matches(trigger, SynonymsFor(recipe, language), message))
            {
                continue;
            }

            var steps = Deserialize<List<RecipeStep>>(recipe.StepsJson);
            if (steps == null || steps.Count == 0)
            {
                _logger.LogWarning("Recipe '{Recipe}' matched but has no steps. Skipping.", recipe.Name);
                continue;
            }

            return new RecipeExecutionPlan(recipe.Name, steps);
        }

        return null;
    }

    public async Task<RecipeExecutionPlan?> ResumeAsync(Guid userId, string conversationId, CancellationToken cancellationToken = default)
    {
        var pending = _pendingRecipeStore.Peek(userId, conversationId);
        if (pending == null)
        {
            return null;
        }

        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IAgentRecipeRepository>();
        var recipe = await repository.GetByNameAsync(pending.RecipeName, cancellationToken);
        if (recipe == null || !recipe.IsEnabled)
        {
            _pendingRecipeStore.Clear(userId, conversationId);
            return null;
        }

        var steps = Deserialize<List<RecipeStep>>(recipe.StepsJson);
        if (steps == null || steps.Count == 0)
        {
            _pendingRecipeStore.Clear(userId, conversationId);
            return null;
        }

        return new RecipeExecutionPlan(
            recipe.Name,
            steps,
            new Dictionary<string, string>(pending.Slots, StringComparer.OrdinalIgnoreCase),
            pending.StepIndex);
    }

    public async Task<IReadOnlyList<string>> GuaranteedSkillNamesAsync(
        string? userId, string? conversationId, string? message,
        string? language = null, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IAgentRecipeRepository>();

        if (Guid.TryParse(userId, out var userGuid) && !string.IsNullOrEmpty(conversationId))
        {
            var pending = _pendingRecipeStore.Peek(userGuid, conversationId);
            if (pending != null)
            {
                var paused = await repository.GetByNameAsync(pending.RecipeName, cancellationToken);
                if (paused != null && paused.IsEnabled)
                {
                    return ExtractStepSkills(paused);
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(message))
        {
            var recipes = await repository.GetAllEnabledAsync(cancellationToken);
            foreach (var recipe in recipes)
            {
                var trigger = Deserialize<RecipeTrigger>(recipe.TriggerJson);
                if (trigger != null && RecipeTriggerMatcher.Matches(trigger, SynonymsFor(recipe, language), message))
                {
                    return ExtractStepSkills(recipe);
                }
            }
        }

        return [];
    }

    private static IReadOnlyCollection<string>? SynonymsFor(AgentRecipe recipe, string? language)
    {
        if (string.IsNullOrEmpty(language) || recipe.Synonyms == null)
        {
            return null;
        }

        // Case-insensitive on the language key so a casing/culture variant ("ES") still resolves, while
        // preserving region-qualified plugin codes such as "zh-CN" (compared, not lowercased).
        foreach (var entry in recipe.Synonyms)
        {
            if (string.Equals(entry.Key, language, StringComparison.OrdinalIgnoreCase))
            {
                return entry.Value;
            }
        }

        return null;
    }

    private static IReadOnlyList<string> ExtractStepSkills(AgentRecipe recipe)
    {
        var steps = Deserialize<List<RecipeStep>>(recipe.StepsJson);
        if (steps == null)
        {
            return [];
        }

        return steps
            .Where(s => !string.IsNullOrWhiteSpace(s.Skill))
            .Select(s => s.Skill!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public void Persist(Guid userId, string conversationId, RecipeExecutionPlan plan)
    {
        _pendingRecipeStore.Save(new PendingRecipe
        {
            UserId = userId,
            ConversationId = conversationId,
            RecipeName = plan.Name,
            StepIndex = plan.StepIndex,
            Slots = new Dictionary<string, string>(plan.Slots, StringComparer.OrdinalIgnoreCase)
        });
    }

    public void Clear(Guid userId, string conversationId)
    {
        _pendingRecipeStore.Clear(userId, conversationId);
    }

    private static T? Deserialize<T>(string? json) where T : class
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
