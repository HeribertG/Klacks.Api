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
using Klacks.Api.KnowledgeIndex.Application.Interfaces;
using Klacks.Api.KnowledgeIndex.Domain;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Domain.Services.Assistant;

public class RecipeEngineService
{
    // Deliberately higher than the skill retrieval score cutoff (0.05): a recipe match commits the
    // user to a multi-turn guided flow, so a semantic fallback (no explicit trigger keyword) needs
    // much stronger confidence than picking a single tool for one turn.
    private const double SemanticMatchScoreThreshold = 0.5;
    private const int SemanticMatchTopK = 3;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IPendingRecipeStore _pendingRecipeStore;
    private readonly ILogger<RecipeEngineService> _logger;

    // Per-request memo (the service is scoped): GuaranteedSkillNamesAsync (tool-catalog assembly) and
    // ResolveAsync (plan building) both match the same message in one turn. Without the memo, a message
    // with no keyword-trigger match would run the semantic fallback (embedding + rerank) twice per turn.
    // Recipes are startup-seeded and stable within a request, so caching the match — including a null
    // miss — is safe.
    private (string Message, string? Language, AgentRecipe? Recipe)? _matchMemo;

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

        var recipe = await FindMatchingRecipeAsync(scope, recipes, message, language, cancellationToken);
        if (recipe == null)
        {
            return null;
        }

        var steps = Deserialize<List<RecipeStep>>(recipe.StepsJson);
        if (steps == null || steps.Count == 0)
        {
            _logger.LogWarning("Recipe '{Recipe}' matched but has no steps. Skipping.", recipe.Name);
            return null;
        }

        return new RecipeExecutionPlan(recipe.Name, steps);
    }

    // Shared by ResolveAsync and GuaranteedSkillNamesAsync so both agree on which recipe (if any)
    // matched a message: the keyword trigger is the deterministic fast path, the semantic fallback
    // only runs when no trigger matched. If the two callers used divergent matching logic, a
    // semantically-resolved recipe's step skills could resolve a plan while never being guaranteed
    // into the tool budget for that turn — the exact gap this method closes.
    private async Task<AgentRecipe?> FindMatchingRecipeAsync(
        IServiceScope scope, List<AgentRecipe> recipes, string message, string? language, CancellationToken cancellationToken)
    {
        if (recipes.Count == 0)
        {
            return null;
        }

        var memo = _matchMemo;
        if (memo != null && memo.Value.Message == message && memo.Value.Language == language)
        {
            return memo.Value.Recipe;
        }

        var match = MatchByTrigger(recipes, message, language)
            ?? await FindMatchingRecipeSemanticAsync(scope, recipes, message, cancellationToken);

        _matchMemo = (message, language, match);
        return match;
    }

    private static AgentRecipe? MatchByTrigger(List<AgentRecipe> recipes, string message, string? language)
    {
        foreach (var recipe in recipes)
        {
            var trigger = Deserialize<RecipeTrigger>(recipe.TriggerJson);
            if (trigger != null && RecipeTriggerMatcher.Matches(trigger, SynonymsFor(recipe, language), message))
            {
                return recipe;
            }
        }

        return null;
    }

    private async Task<AgentRecipe?> FindMatchingRecipeSemanticAsync(
        IServiceScope scope, List<AgentRecipe> recipes, string message, CancellationToken cancellationToken)
    {
        var retrieval = scope.ServiceProvider.GetRequiredService<IKnowledgeRetrievalService>();

        RetrievalResult result;
        try
        {
            result = await retrieval.RetrieveAsync(message, [], isAdmin: false, SemanticMatchTopK, currentRoute: null, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Semantic recipe fallback failed; continuing without a recipe match.");
            return null;
        }

        var recipeHit = result.Candidates
            .Where(c => c.Entry.Kind == KnowledgeEntryKind.Recipe && c.Score >= SemanticMatchScoreThreshold)
            .OrderByDescending(c => c.Score)
            .FirstOrDefault();

        if (recipeHit == null)
        {
            return null;
        }

        var recipe = recipes.FirstOrDefault(r =>
            string.Equals(r.Name, recipeHit.Entry.SourceId, StringComparison.OrdinalIgnoreCase));
        if (recipe != null)
        {
            _logger.LogInformation(
                "Recipe '{Recipe}' matched via semantic fallback (score={Score:F3})", recipe.Name, recipeHit.Score);
        }

        return recipe;
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
            var recipe = await FindMatchingRecipeAsync(scope, recipes, message, language, cancellationToken);
            if (recipe != null)
            {
                return ExtractStepSkills(recipe);
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
