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
    // Single grey-zone floor for the semantic fallback (no explicit trigger keyword), deliberately far
    // above the skill retrieval cutoff (0.05): a recipe match commits the user to a multi-turn guided
    // flow, so EVERY match above the floor — high score or grey zone — runs through the confirmation
    // gate; the gate question, not the score, is the safety net. The floor is set low (0.4) on purpose
    // because cross-lingual queries score lower against the de/en embedding text and would otherwise
    // never surface. Below the floor nothing matches. A runner-up within the ambiguity margin is
    // surfaced as an alternative in the gate question instead of being silently discarded.
    // HighConfidenceLogThreshold is LOGGING ONLY (a calibration label in the match log, no behavioral
    // branch): it lets us see from the logs how many matches would survive a higher floor. All candidate
    // scores are logged for threshold calibration.
    private const double SemanticHighConfidenceLogThreshold = 0.7;
    private const double SemanticGreyZoneThreshold = 0.4;
    private const double SemanticAmbiguityMargin = 0.05;
    private const int SemanticMatchTopK = 3;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IPendingRecipeStore _pendingRecipeStore;
    private readonly ILogger<RecipeEngineService> _logger;

    // Per-request memo (the service is scoped): GuaranteedSkillNamesAsync (tool-catalog assembly) and
    // ResolveAsync (plan building) both match the same message in one turn. Without the memo, a message
    // with no keyword-trigger match would run the semantic fallback (embedding + rerank) twice per turn.
    // Recipes are startup-seeded and stable within a request, so caching the match — including a null
    // miss — is safe. MatchedSemantically must be cached alongside the recipe, not just the recipe
    // itself: ResolveAsync uses it to decide whether the plan needs a confirmation gate, and a cache hit
    // that dropped this flag would silently skip the gate for a semantically-matched recipe.
    private (string Message, string? Language, AgentRecipe? Recipe, bool MatchedSemantically, string? AlternativeGoal)? _matchMemo;

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

        var (recipe, matchedSemantically, alternativeGoal) = await FindMatchingRecipeAsync(scope, recipes, message, language, cancellationToken);
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

        return new RecipeExecutionPlan(
            recipe.Name, steps, needsConfirmation: matchedSemantically, goal: recipe.Goal, alternativeGoal: alternativeGoal);
    }

    // Shared by ResolveAsync and GuaranteedSkillNamesAsync so both agree on which recipe (if any)
    // matched a message: the keyword trigger is the deterministic fast path, the semantic fallback
    // only runs when no trigger matched. If the two callers used divergent matching logic, a
    // semantically-resolved recipe's step skills could resolve a plan while never being guaranteed
    // into the tool budget for that turn — the exact gap this method closes. The bool flags whether the
    // match came from the semantic fallback (as opposed to the deterministic keyword trigger) so
    // ResolveAsync can gate a semantic match behind a user confirmation before forcing its steps.
    private async Task<(AgentRecipe? Recipe, bool MatchedSemantically, string? AlternativeGoal)> FindMatchingRecipeAsync(
        IServiceScope scope, List<AgentRecipe> recipes, string message, string? language, CancellationToken cancellationToken)
    {
        if (recipes.Count == 0)
        {
            return (null, false, null);
        }

        var memo = _matchMemo;
        if (memo != null && memo.Value.Message == message && memo.Value.Language == language)
        {
            return (memo.Value.Recipe, memo.Value.MatchedSemantically, memo.Value.AlternativeGoal);
        }

        var triggerMatch = MatchByTrigger(recipes, message, language);
        var (semanticMatch, alternativeGoal) = triggerMatch == null
            ? await FindMatchingRecipeSemanticAsync(scope, recipes, message, cancellationToken)
            : ((AgentRecipe?)null, (string?)null);
        var match = triggerMatch ?? semanticMatch;
        var matchedSemantically = triggerMatch == null && match != null;

        _matchMemo = (message, language, match, matchedSemantically, alternativeGoal);
        return (match, matchedSemantically, alternativeGoal);
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

    private async Task<(AgentRecipe? Recipe, string? AlternativeGoal)> FindMatchingRecipeSemanticAsync(
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
            return (null, null);
        }

        var recipeCandidates = result.Candidates
            .Where(c => c.Entry.Kind == KnowledgeEntryKind.Recipe)
            .OrderByDescending(c => c.Score)
            .ToList();

        if (recipeCandidates.Count > 0)
        {
            _logger.LogInformation(
                "Semantic recipe candidate scores (calibration): {Scores}",
                string.Join(", ", recipeCandidates.Select(c => $"{c.Entry.SourceId}={c.Score:F3}")));
        }

        var top = recipeCandidates.FirstOrDefault();
        if (top == null || top.Score < SemanticGreyZoneThreshold)
        {
            return (null, null);
        }

        var recipe = recipes.FirstOrDefault(r =>
            string.Equals(r.Name, top.Entry.SourceId, StringComparison.OrdinalIgnoreCase));
        if (recipe == null)
        {
            return (null, null);
        }

        var runnerUp = recipeCandidates
            .Skip(1)
            .FirstOrDefault(c => c.Score >= SemanticGreyZoneThreshold
                                 && top.Score - c.Score < SemanticAmbiguityMargin
                                 && !string.Equals(c.Entry.SourceId, top.Entry.SourceId, StringComparison.OrdinalIgnoreCase));

        string? alternativeGoal = null;
        if (runnerUp != null)
        {
            var alternativeRecipe = recipes.FirstOrDefault(r =>
                string.Equals(r.Name, runnerUp.Entry.SourceId, StringComparison.OrdinalIgnoreCase));
            alternativeGoal = alternativeRecipe?.Goal ?? runnerUp.Entry.SourceId;
        }

        _logger.LogInformation(
            "Recipe '{Recipe}' matched via semantic fallback (score={Score:F3}, confidence={Confidence}, alternative={Alternative})",
            recipe.Name,
            top.Score,
            top.Score >= SemanticHighConfidenceLogThreshold ? "high" : "grey-zone",
            alternativeGoal ?? "none");

        return (recipe, alternativeGoal);
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
            pending.StepIndex,
            needsConfirmation: pending.AwaitingConfirmation,
            goal: recipe.Goal,
            captureRewindUsed: pending.CaptureRewindUsed);
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
            var (recipe, _, _) = await FindMatchingRecipeAsync(scope, recipes, message, language, cancellationToken);
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
            Slots = new Dictionary<string, string>(plan.Slots, StringComparer.OrdinalIgnoreCase),
            AwaitingConfirmation = plan.NeedsConfirmation,
            CaptureRewindUsed = plan.CaptureRewindUsed
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
