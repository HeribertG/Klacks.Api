// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Resolves and resumes data-driven recipes for the chat loop. ResolveAsync matches the message
/// against the enabled recipes (loaded from the database) and builds a fresh execution plan; ResumeAsync
/// rebuilds the plan paused on an ask step from the pending store. Persist/Clear manage the durable slot
/// bag across ask turns. The recipe definitions live in the database, so new recipes are added without a
/// recompile (seeded from recipe-seeds.json, editable directly in the table). A keyword-trigger match is
/// additionally checked against competing skill intents (ICompetingSkillIntentDetector): when the message
/// verbatim contains a foreign skill's multiword trigger phrase the trigger does not own, the plan runs
/// through the confirmation gate instead of hijacking the turn silently.
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
    // that dropped this flag would silently skip the gate for a semantically-matched recipe. The same
    // holds for HasCompetingSkillIntent: it also feeds the confirmation gate decision.
    private (string Message, string? Language, AgentRecipe? Recipe, bool MatchedSemantically, bool HasCompetingSkillIntent, string? AlternativeGoal, Dictionary<string, string>? AlternativeGoalTranslations)? _matchMemo;

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
        string? message, string? language = null,
        IReadOnlyCollection<string>? userRights = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return null;
        }

        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IAgentRecipeRepository>();
        var recipes = await repository.GetAllEnabledAsync(cancellationToken);

        var (recipe, matchedSemantically, hasCompetingSkillIntent, alternativeGoal, alternativeGoalTranslations) =
            await FindMatchingRecipeAsync(scope, recipes, message, language, userRights, cancellationToken);
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
            recipe.Name,
            steps,
            needsConfirmation: matchedSemantically || hasCompetingSkillIntent,
            goal: recipe.Goal,
            alternativeGoal: alternativeGoal,
            goalTranslations: recipe.GoalTranslations,
            alternativeGoalTranslations: alternativeGoalTranslations);
    }

    // Shared by ResolveAsync and GuaranteedSkillNamesAsync so both agree on which recipe (if any)
    // matched a message: the keyword trigger is the deterministic fast path, the semantic fallback
    // only runs when no trigger matched. If the two callers used divergent matching logic, a
    // semantically-resolved recipe's step skills could resolve a plan while never being guaranteed
    // into the tool budget for that turn — the exact gap this method closes. The bool flags whether the
    // match came from the semantic fallback (as opposed to the deterministic keyword trigger) so
    // ResolveAsync can gate a semantic match behind a user confirmation before forcing its steps.
    private async Task<(AgentRecipe? Recipe, bool MatchedSemantically, bool HasCompetingSkillIntent, string? AlternativeGoal, Dictionary<string, string>? AlternativeGoalTranslations)> FindMatchingRecipeAsync(
        IServiceScope scope, List<AgentRecipe> recipes, string message, string? language,
        IReadOnlyCollection<string>? userRights, CancellationToken cancellationToken)
    {
        if (recipes.Count == 0)
        {
            return (null, false, false, null, null);
        }

        var memo = _matchMemo;
        if (memo != null && memo.Value.Message == message && memo.Value.Language == language)
        {
            return (memo.Value.Recipe, memo.Value.MatchedSemantically, memo.Value.HasCompetingSkillIntent, memo.Value.AlternativeGoal, memo.Value.AlternativeGoalTranslations);
        }

        // Every recipe is a guided MUTATION flow (create/add/onboard/setup/bulk). The semantic fallback
        // ranks a message against those recipes by topic similarity, which conflates "talks about groups"
        // with "wants to mutate groups": a read question like "Welche Gruppen gibt es?" scores 0.6+ against
        // create-group and would hijack the turn into a confirmation gate. Suppress the fallback for plain
        // information questions (multilingual question-lead detection: core-language leads plus per-plugin
        // leads, incl. CJK prefixes). A negative gate on purpose — verbless action requests ("Neuen
        // Mitarbeiter, bitte") carry no mutation verb yet must still reach the guided flow, so gating on a
        // positive mutation signal would starve exactly the terse phrasings recipes exist to catch. The
        // deterministic keyword trigger is unaffected — an explicit trigger phrase matches without a check.
        // The same suppression applies to replies that LEAD with a negation ("Nein, nein", "No, not
        // now"): they decline an offer the assistant just made, carry no action intent, and would
        // otherwise rank into the grey zone of a mutation recipe and hijack the turn into a
        // confirmation gate. A mutation verb after the negation ("Nein, erstelle stattdessen ...")
        // re-enables the fallback because the negation then corrects course instead of declining.
        var triggerMatch = MatchByTrigger(recipes, message, language);
        var isLeadingDecline = DeclineDetector.LeadsWithNegation(message)
                               && !MutationIntentDetector.IsMutationIntent(message);
        var runSemanticFallback = triggerMatch == null
                                  && !MutationIntentDetector.IsInformationQuestion(message)
                                  && !isLeadingDecline;
        var (semanticMatch, alternativeGoal, alternativeGoalTranslations) = runSemanticFallback
            ? await FindMatchingRecipeSemanticAsync(scope, recipes, message, cancellationToken)
            : ((AgentRecipe?)null, (string?)null, (Dictionary<string, string>?)null);
        var match = triggerMatch?.Recipe ?? semanticMatch;
        var matchedSemantically = triggerMatch == null && match != null;
        var hasCompetingSkillIntent = triggerMatch != null
            && await HasCompetingSkillIntentAsync(scope, triggerMatch.Value, message, language, userRights, cancellationToken);

        _matchMemo = (message, language, match, matchedSemantically, hasCompetingSkillIntent, alternativeGoal, alternativeGoalTranslations);
        return (match, matchedSemantically, hasCompetingSkillIntent, alternativeGoal, alternativeGoalTranslations);
    }

    private static (AgentRecipe Recipe, RecipeTrigger Trigger, IReadOnlyCollection<string>? Synonyms)? MatchByTrigger(
        List<AgentRecipe> recipes, string message, string? language)
    {
        foreach (var recipe in recipes)
        {
            var trigger = Deserialize<RecipeTrigger>(recipe.TriggerJson);
            var synonyms = SynonymsFor(recipe, language);
            if (trigger != null && RecipeTriggerMatcher.Matches(trigger, synonyms, message))
            {
                return (recipe, trigger, synonyms);
            }
        }

        return null;
    }

    // The keyword trigger is deterministic but bag-of-substrings: a long message can satisfy a
    // recipe's verb+noun conditions through incidental vocabulary while the user explicitly named a
    // different skill's action (live regression 2026-07-16: "neue Firmenregel: maximal 3
    // Nachtschichten ..." matched create-shift-order). When a foreign skill phrase competes, the
    // plan is routed through the existing confirmation gate instead of hijacking the turn silently —
    // a recoverable question instead of a silent misroute. Detection failures degrade to the
    // previous silent-start behavior: the safety net must never take the recipe engine down.
    private async Task<bool> HasCompetingSkillIntentAsync(
        IServiceScope scope,
        (AgentRecipe Recipe, RecipeTrigger Trigger, IReadOnlyCollection<string>? Synonyms) match,
        string message,
        string? language,
        IReadOnlyCollection<string>? userRights,
        CancellationToken cancellationToken)
    {
        var servedSkills = ExtractStepSkills(match.Recipe);

        var oldDecision = false;
        IReadOnlyList<string> competing = [];
        try
        {
            var detector = scope.ServiceProvider.GetRequiredService<ICompetingSkillIntentDetector>();
            competing = await detector.FindCompetingSkillNamesAsync(
                message, language, match.Trigger, match.Synonyms, servedSkills, cancellationToken);
            oldDecision = competing.Count > 0;
            if (oldDecision)
            {
                _logger.LogWarning(
                    "Recipe '{Recipe}' keyword-trigger match competes with skill intent(s) {Skills}; gating the plan behind confirmation.",
                    match.Recipe.Name, string.Join(", ", competing));
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Competing skill intent detection failed; starting recipe '{Recipe}' directly.", match.Recipe.Name);
            oldDecision = false;
        }

        // Shadow-mode margin signal: computed and logged ONLY, on EVERY keyword match (independent of the
        // legacy decision above), so the two signals' divergence — e.g. a compound word the substring net
        // misses but the margin catches — is recorded for calibration. Its own try/catch, isolated from the
        // routing decision: a shadow failure must never alter oldDecision (constraint: routing unchanged).
        try
        {
            var evaluator = scope.ServiceProvider.GetRequiredService<IRecipeSkillMarginEvaluator>();
            await evaluator.EvaluateAndLogAsync(
                new RecipeSkillMarginRequest(
                    message,
                    match.Recipe.Name,
                    DescribeTrigger(match.Trigger),
                    servedSkills,
                    oldDecision,
                    competing,
                    userRights),
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex, "[recipe-skill-margin] shadow margin evaluation failed for recipe '{Recipe}'; routing unaffected.", match.Recipe.Name);
        }

        return oldDecision;
    }

    // Compact, log-friendly rendering of a recipe trigger's positive (allOf) keyword conditions. Diagnostic
    // only — feeds the shadow log's matchedTrigger field.
    private static string DescribeTrigger(RecipeTrigger trigger)
    {
        var terms = trigger.AllOf
            .SelectMany(c => (c.AnyWordStart ?? Enumerable.Empty<string>())
                .Concat(c.AnySubstring ?? Enumerable.Empty<string>())
                .Concat(c.StartsWith ?? Enumerable.Empty<string>()))
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .ToList();

        return terms.Count == 0 ? "(none)" : string.Join("|", terms);
    }

    private async Task<(AgentRecipe? Recipe, string? AlternativeGoal, Dictionary<string, string>? AlternativeGoalTranslations)> FindMatchingRecipeSemanticAsync(
        IServiceScope scope, List<AgentRecipe> recipes, string message, CancellationToken cancellationToken)
    {
        var retrieval = scope.ServiceProvider.GetRequiredService<IKnowledgeRetrievalService>();

        RetrievalResult result;
        try
        {
            result = await retrieval.RetrieveAsync(
                message, [], isAdmin: false, SemanticMatchTopK, currentRoute: null, cancellationToken,
                KnowledgeEntryKind.Recipe);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Semantic recipe fallback failed; continuing without a recipe match.");
            return (null, null, null);
        }

        // Restricted to recipes and ordered by score inside RetrieveAsync. Filtering here instead
        // would arrive too late: topK is applied there, so the recipes would already be gone.
        var recipeCandidates = result.Candidates;

        if (recipeCandidates.Count > 0)
        {
            _logger.LogInformation(
                "Semantic recipe candidate scores (calibration): {Scores}",
                string.Join(", ", recipeCandidates.Select(c => $"{c.Entry.SourceId}={c.Score:F3}")));
        }

        // A recipe's noneOf guard is its authored exclusion vocabulary ("never fire for messages
        // containing these terms"). The keyword trigger honors it inside RecipeTriggerMatcher; the
        // semantic path must honor it too, otherwise a message the author explicitly excluded (e.g. a
        // company-rule intake mentioning shifts) re-enters the recipe flow through the embedding
        // ranking and hijacks the turn into a confirmation gate.
        RetrievalCandidate? top = null;
        AgentRecipe? recipe = null;
        var topIndex = -1;
        for (var i = 0; i < recipeCandidates.Count; i++)
        {
            var candidate = recipeCandidates[i];
            if (candidate.Score < SemanticGreyZoneThreshold)
            {
                break;
            }

            var resolved = FindRecipeByName(recipes, candidate.Entry.SourceId);
            if (resolved == null)
            {
                continue;
            }

            if (IsVetoedByNoneOfGuard(resolved, message))
            {
                _logger.LogInformation(
                    "Recipe '{Recipe}' semantic candidate (score={Score:F3}) vetoed by its noneOf guard.",
                    resolved.Name, candidate.Score);
                continue;
            }

            top = candidate;
            recipe = resolved;
            topIndex = i;
            break;
        }

        if (top == null || recipe == null)
        {
            return (null, null, null);
        }

        string? alternativeGoal = null;
        Dictionary<string, string>? alternativeGoalTranslations = null;
        foreach (var candidate in recipeCandidates.Skip(topIndex + 1))
        {
            if (candidate.Score < SemanticGreyZoneThreshold
                || top.Score - candidate.Score >= SemanticAmbiguityMargin)
            {
                break;
            }

            if (string.Equals(candidate.Entry.SourceId, top.Entry.SourceId, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var alternativeRecipe = FindRecipeByName(recipes, candidate.Entry.SourceId);
            if (alternativeRecipe != null && IsVetoedByNoneOfGuard(alternativeRecipe, message))
            {
                continue;
            }

            alternativeGoal = alternativeRecipe?.Goal ?? candidate.Entry.SourceId;
            alternativeGoalTranslations = alternativeRecipe?.GoalTranslations;
            break;
        }

        _logger.LogInformation(
            "Recipe '{Recipe}' matched via semantic fallback (score={Score:F3}, confidence={Confidence}, alternative={Alternative})",
            recipe.Name,
            top.Score,
            top.Score >= SemanticHighConfidenceLogThreshold ? "high" : "grey-zone",
            alternativeGoal ?? "none");

        return (recipe, alternativeGoal, alternativeGoalTranslations);
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
            captureRewindUsed: pending.CaptureRewindUsed,
            goalTranslations: recipe.GoalTranslations);
    }

    public async Task<IReadOnlyList<string>> GuaranteedSkillNamesAsync(
        string? userId, string? conversationId, string? message,
        string? language = null, IReadOnlyCollection<string>? userRights = null,
        CancellationToken cancellationToken = default)
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
            var (recipe, _, _, _, _) = await FindMatchingRecipeAsync(scope, recipes, message, language, userRights, cancellationToken);
            if (recipe != null)
            {
                return ExtractStepSkills(recipe);
            }
        }

        return [];
    }

    private static AgentRecipe? FindRecipeByName(List<AgentRecipe> recipes, string? name)
        => recipes.FirstOrDefault(r => string.Equals(r.Name, name, StringComparison.OrdinalIgnoreCase));

    private static bool IsVetoedByNoneOfGuard(AgentRecipe recipe, string message)
        => RecipeTriggerMatcher.IsVetoed(Deserialize<RecipeTrigger>(recipe.TriggerJson), message);

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
