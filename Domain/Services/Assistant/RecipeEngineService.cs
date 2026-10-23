// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Resolves and resumes data-driven recipes for the chat loop. ResolveAsync matches the message
/// against the enabled recipes (loaded from the database) and builds a fresh execution plan; ResumeAsync
/// rebuilds the plan paused on an ask step from the pending store. Persist/Clear manage the durable slot
/// bag across ask turns. The recipe definitions live in the database, so new recipes are added without a
/// recompile (seeded from recipe-seeds.json, editable directly in the table). EVERY match, keyword trigger or
/// semantic fallback, runs through the confirmation gate: a recipe never starts without the user's yes, because
/// bag-of-substrings triggers hijack turns the user meant differently. A matched recipe whose step skills the user may
/// not call is not offered at all (RecipeRightsGate), and a refusal wins: the recipe that was matched first, by keyword
/// trigger or, failing that, by the best semantic candidate that passed its veto and anchor, comes back as
/// RecipeResolution.Denial and no other recipe is offered in its place. The rights are part of the match memo key, so a
/// scope that serves several users never hands one user the match computed for another.
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
    // Single grey-zone floor for the semantic fallback (no explicit trigger keyword), deliberately far
    // above the skill retrieval cutoff (KnowledgeIndexConstants.DefaultScoreCutoff, 0.0001): a recipe match commits the user to a multi-turn guided
    // flow, so EVERY match above the floor — high score or grey zone — runs through the confirmation
    // gate; the gate question, not the score, is the safety net. The floor is set low (0.4) on purpose
    // because cross-lingual queries score lower against the de/en embedding text and would otherwise
    // never surface. Below the floor nothing matches. A runner-up within the ambiguity margin is
    // surfaced as an alternative in the gate question instead of being silently discarded.
    // HighConfidenceLogThreshold is LOGGING ONLY (a calibration label in the match log, no behavioral
    // branch): it lets us see from the logs how many matches would survive a higher floor. All candidate
    // scores are logged for threshold calibration.
    // KnowledgeIndexRecipeGoldenSetDiHostTests mirrors these four values to report the gate
    // distribution. Change one here and the probe keeps measuring the old boundary until it is
    // updated too - the copy is deliberate, so the measurement build never depends on this file.
    private const double SemanticHighConfidenceLogThreshold = 0.7;
    private const double SemanticGreyZoneThreshold = 0.4;
    private const double SemanticAmbiguityMargin = 0.05;
    private const int SemanticMatchTopK = 3;
    private const string RightsKeySeparator = "|";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IPendingRecipeStore _pendingRecipeStore;
    private readonly ILogger<RecipeEngineService> _logger;

    // Per-request memo (the service is scoped): GuaranteedSkillNamesAsync (tool-catalog assembly) and
    // ResolveAsync (plan building) both match the same message in one turn. Without the memo, a message
    // with no keyword-trigger match would run the semantic fallback (embedding + rerank) twice per turn.
    // Recipes are startup-seeded and stable within a request, so caching the match — including a null
    // miss — is safe. MatchedSemantically must be cached alongside the recipe, not just the recipe
    // itself: the confirmation question tells the user the flow was found by meaning, and a cache hit that
    // dropped this flag would word it as an explicit trigger. The denial of a refused match is cached alongside it.
    // ExcludedRecipeName is part of the KEY, not only of the payload: a call that excludes the recipe the
    // user just corrected must neither be served from nor overwrite the entry a call without that
    // exclusion produced for the same text. Same reason MatchedSemantically is cached alongside the recipe
    // rather than recomputed.
    private (string Message, string? Language, string? ExcludedRecipeName, string? RightsKey, RecipeMatch Match)? _matchMemo;

    /// <summary>
    /// What matching a message produced: the recipe (or null), how it was found, the second flow of an ambiguous
    /// semantic match and, when the only matches were refused for missing rights, the first refused one. A refused
    /// match has a null recipe and a non-null denial.
    /// </summary>
    private sealed record RecipeMatch(
        AgentRecipe? Recipe,
        bool MatchedSemantically,
        string? AlternativeGoal,
        Dictionary<string, string>? AlternativeGoalTranslations,
        RecipeDenial? Denial)
    {
        internal static readonly RecipeMatch None = new(null, false, null, null, null);
    }

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

    /// <param name="excludedRecipeName">
    /// Recipe to skip while matching, used after a correction aborted the pending one. Last parameter on
    /// purpose rather than before cancellationToken: every existing caller - both chat loops, the
    /// capability learner and the engine's own tests - passes the first four positionally, and inserting a
    /// parameter among them would rebind cancellationToken instead of failing to compile. The ordering is
    /// unconventional and deliberate.
    /// </param>
    public async Task<RecipeExecutionPlan?> ResolveAsync(
        string? message, string? language = null,
        IReadOnlyCollection<string>? userRights = null, CancellationToken cancellationToken = default,
        string? excludedRecipeName = null) =>
        (await ResolveWithDenialAsync(message, language, userRights, cancellationToken, excludedRecipeName)).Plan;

    /// <summary>
    /// ResolveAsync plus the reason a match was refused: when the message matched a recipe whose step skills the user may
    /// not call and nothing else could be offered, the plan is null and the denial names the recipe and the missing
    /// rights. Same parameters as ResolveAsync.
    /// </summary>
    public Task<RecipeResolution> ResolveWithDenialAsync(
        string? message, string? language = null,
        IReadOnlyCollection<string>? userRights = null, CancellationToken cancellationToken = default,
        string? excludedRecipeName = null) =>
        ResolveWithDenialAsync(message, language, userRights, excludedRecipeName, allowSemanticFallback: true, cancellationToken);

    /// <summary>
    /// ResolveWithDenialAsync with the semantic fallback switchable. False restricts matching to the deterministic
    /// keyword trigger: used for the reply that turned down a pending confirmation, where a similarity score over a
    /// short follow-up ("pick them yourself") says nothing about a new request and used to start an unrelated flow.
    /// Full arity and no defaults on purpose, for the same overload-rebinding reason as GuaranteedSkillNamesAsync.
    /// </summary>
    public async Task<RecipeResolution> ResolveWithDenialAsync(
        string? message, string? language,
        IReadOnlyCollection<string>? userRights, string? excludedRecipeName,
        bool allowSemanticFallback, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return new RecipeResolution(null, null);
        }

        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IAgentRecipeRepository>();
        var recipes = await repository.GetAllEnabledAsync(cancellationToken);

        var match = await FindMatchingRecipeAsync(
            scope, recipes, message, language, userRights, cancellationToken, excludedRecipeName, allowSemanticFallback);
        var recipe = match.Recipe;
        if (recipe == null)
        {
            return new RecipeResolution(null, match.Denial);
        }

        var steps = Deserialize<List<RecipeStep>>(recipe.StepsJson);
        if (steps == null || steps.Count == 0)
        {
            _logger.LogWarning("Recipe '{Recipe}' matched but has no steps. Skipping.", recipe.Name);
            return new RecipeResolution(null, null);
        }

        // W5.3: pre-fill slots the first message already contains (times, dates, weekdays) so the
        // plan skips those ask steps ("ask whose slot is filled"). Deliberately no group/name
        // extraction here: entity names must be validated against the database and are asked for or
        // guarded at execution time instead of being trusted from free text.
        var initialSlots = RecipeInitialSlotExtractor.Extract(
            message,
            steps
                .Where(step => !string.IsNullOrWhiteSpace(step.Slot))
                .Select(step => step.Slot!)
                .ToList());

        return new RecipeResolution(
            new RecipeExecutionPlan(
                recipe.Name,
                steps,
                slots: initialSlots,
                needsConfirmation: true,
                goal: recipe.Goal,
                alternativeGoal: match.AlternativeGoal,
                goalTranslations: recipe.GoalTranslations,
                alternativeGoalTranslations: match.AlternativeGoalTranslations,
                triggerMessage: message,
                matchedByMeaning: match.MatchedSemantically),
            null);
    }

    // Shared by ResolveAsync and GuaranteedSkillNamesAsync so both agree on which recipe (if any)
    // matched a message: the keyword trigger is the deterministic fast path, the semantic fallback
    // only runs when no trigger matched. If the two callers used divergent matching logic, a
    // semantically-resolved recipe's step skills could resolve a plan while never being guaranteed
    // into the tool budget for that turn — the exact gap this method closes. MatchedSemantically flags whether the
    // match came from the semantic fallback (as opposed to the deterministic keyword trigger) so the confirmation
    // question can say so; both kinds are confirmed before any step is forced.
    private async Task<RecipeMatch> FindMatchingRecipeAsync(
        IServiceScope scope, List<AgentRecipe> recipes, string message, string? language,
        IReadOnlyCollection<string>? userRights, CancellationToken cancellationToken,
        string? excludedRecipeName = null,
        bool allowSemanticFallback = true)
    {
        if (recipes.Count == 0)
        {
            return RecipeMatch.None;
        }

        var rightsKey = RightsKey(userRights);
        var memo = allowSemanticFallback ? _matchMemo : null;
        if (memo != null && memo.Value.Message == message && memo.Value.Language == language
                         && memo.Value.ExcludedRecipeName == excludedRecipeName && memo.Value.RightsKey == rightsKey)
        {
            return memo.Value.Match;
        }

        // Filtered once, here, so both matching paths inherit it: the deterministic trigger walk AND the
        // semantic fallback, including the two noneOf veto checks inside it that resolve candidates by name
        // out of this same list. Excluding inside only one of the paths would leave the other free to hand
        // back the recipe the user just corrected - and for a correction, re-matching it is the worst
        // outcome, because the plan would restart at step 0 with every collected slot discarded.
        var eligible = string.IsNullOrWhiteSpace(excludedRecipeName)
            ? recipes
            : recipes.Where(r => !string.Equals(r.Name, excludedRecipeName, StringComparison.OrdinalIgnoreCase)).ToList();
        if (eligible.Count == 0)
        {
            return RecipeMatch.None;
        }

        // The rights check runs on the recipe the message matched, never on the whole catalogue, so a message that
        // matches nothing costs no query. A refusal wins: the first keyword-trigger match is the recipe the message
        // points at, and offering another flow in its place would send the user somewhere they did not ask to go, so it
        // is returned as the denial the turn tells the model about. userRights == null means a probe that asks whether
        // a message routes at all, which is not a question about one user's rights.
        var rightsGate = userRights == null
            ? null
            : new RecipeRightsGate(scope.ServiceProvider.GetRequiredService<IAgentSkillRepository>(), userRights);

        var triggerMatch = MatchByTrigger(eligible, message, language, _logger);
        if (triggerMatch != null && rightsGate != null)
        {
            var refused = await rightsGate.CheckAsync(triggerMatch, cancellationToken);
            if (refused != null)
            {
                return Remember(
                    new RecipeMatch(null, false, null, null, LogDenial(refused)), message, language, excludedRecipeName, rightsKey, allowSemanticFallback);
            }
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
        // Symmetrically, a reply that LEADS with an affirmation ("Ja, übernimm die Gruppen so", "Yes, apply
        // it") accepts the offer the assistant just made: it carries no new recipe intent, yet its topic words
        // rank it into the grey zone of group recipes (live 2026-10-03: prepare-groups-for-planning hijacked
        // the confirmation of a partition_clients_by_address preview). Only a leading affirmation counts, so a
        // terse request with a courtesy word ("Neuen Mitarbeiter, bitte") still reaches the fallback. The
        // keyword trigger still applies.
        var isLeadingDecline = DeclineDetector.LeadsWithNegation(message, language)
                               && !MutationIntentDetector.IsMutationIntent(message);
        var runSemanticFallback = allowSemanticFallback
                                  && triggerMatch == null
                                  && !MutationIntentDetector.IsInformationQuestion(message)
                                  && !isLeadingDecline
                                  && !AffirmationDetector.LeadsWithAffirmation(message, language);
        var (semanticMatch, alternativeGoal, alternativeGoalTranslations, semanticDenial) = runSemanticFallback
            ? await FindMatchingRecipeSemanticAsync(scope, eligible, message, language, rightsGate, cancellationToken)
            : ((AgentRecipe?)null, (string?)null, (Dictionary<string, string>?)null, (RecipeDenial?)null);

        var match = triggerMatch ?? semanticMatch;
        var result = new RecipeMatch(
            match,
            MatchedSemantically: triggerMatch == null && match != null,
            alternativeGoal,
            alternativeGoalTranslations,
            match == null && semanticDenial != null ? LogDenial(semanticDenial) : null);

        return Remember(result, message, language, excludedRecipeName, rightsKey, allowSemanticFallback);
    }

    private RecipeMatch Remember(
        RecipeMatch result, string message, string? language, string? excludedRecipeName, string? rightsKey,
        bool allowSemanticFallback)
    {
        if (allowSemanticFallback)
        {
            _matchMemo = (message, language, excludedRecipeName, rightsKey, result);
        }

        return result;
    }

    private static string? RightsKey(IReadOnlyCollection<string>? userRights) =>
        userRights == null ? null : string.Join(RightsKeySeparator, userRights.OrderBy(right => right, StringComparer.Ordinal));

    private RecipeDenial LogDenial(RecipeDenial denial)
    {
        _logger.LogInformation(
            "Recipe '{Recipe}' matched but is not offered: the user lacks {MissingRights} needed by its step skills.",
            denial.RecipeName, string.Join(", ", denial.MissingRights));
        return denial;
    }

    private static AgentRecipe? MatchByTrigger(
        List<AgentRecipe> recipes, string message, string? language, ILogger logger)
    {
        foreach (var recipe in recipes)
        {
            var trigger = Deserialize<RecipeTrigger>(recipe.TriggerJson);
            var synonyms = recipe.SynonymsFor(language);
            if (trigger != null && RecipeTriggerMatcher.Matches(
                    trigger, synonyms, message, logger, language, recipe.VetoesFor(language)))
            {
                return recipe;
            }
        }

        return null;
    }
    private async Task<(AgentRecipe? Recipe, string? AlternativeGoal, Dictionary<string, string>? AlternativeGoalTranslations, RecipeDenial? Denial)> FindMatchingRecipeSemanticAsync(
        IServiceScope scope, List<AgentRecipe> recipes, string message, string? language,
        RecipeRightsGate? rightsGate, CancellationToken cancellationToken)
    {
        var retriever = scope.ServiceProvider.GetRequiredService<IRecipeCandidateRetriever>();

        IReadOnlyList<RecipeCandidate> recipeCandidates;
        try
        {
            // The raw message, deliberately - not the history-widened query the toolset assembler
            // builds through RetrievalQueryBuilder. A recipe answers what the user is asking for now;
            // folding earlier turns in would let a finished topic keep proposing its recipe. The two
            // passes therefore ask different questions, which is why their scores are not comparable.
            recipeCandidates = await retriever.RetrieveAsync(message, SemanticMatchTopK, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Semantic recipe fallback failed; continuing without a recipe match.");
            return (null, null, null, null);
        }

        // Restricted to recipes and ordered by score inside RetrieveAsync. Filtering here instead
        // would arrive too late: topK is applied there, so the recipes would already be gone.
        if (recipeCandidates.Count > 0)
        {
            _logger.LogInformation(
                "Semantic recipe candidate scores (calibration): {Scores}",
                string.Join(", ", recipeCandidates.Select(c => $"{c.RecipeName}={c.Score:F3}")));
        }

        // A recipe's noneOf guard is its authored exclusion vocabulary ("never fire for messages
        // containing these terms"). The keyword trigger honors it inside RecipeTriggerMatcher; the
        // semantic path must honor it too, otherwise a message the author explicitly excluded (e.g. a
        // company-rule intake mentioning shifts) re-enters the recipe flow through the embedding
        // ranking and hijacks the turn into a confirmation gate. A candidate that passed both guards is a real match: if the
        // user's rights refuse it, the loop ends with that refusal instead of walking on to the next, lower-ranked recipe.
        RecipeCandidate? top = null;
        AgentRecipe? recipe = null;
        var topIndex = -1;
        for (var i = 0; i < recipeCandidates.Count; i++)
        {
            var candidate = recipeCandidates[i];
            if (candidate.Score < SemanticGreyZoneThreshold)
            {
                break;
            }

            var resolved = FindRecipeByName(recipes, candidate.RecipeName);
            if (resolved == null)
            {
                continue;
            }

            if (IsVetoedByNoneOfGuard(resolved, message, _logger, language))
            {
                _logger.LogInformation(
                    "Recipe '{Recipe}' semantic candidate (score={Score:F3}) vetoed by its noneOf guard.",
                    resolved.Name, candidate.Score);
                continue;
            }

            if (!HasSemanticAnchor(resolved, message, _logger, language))
            {
                _logger.LogInformation(
                    "Recipe '{Recipe}' semantic candidate (score={Score:F3}) rejected: the message names its subject neither in the core trigger nor in any installed language-pack anchor.",
                    resolved.Name, candidate.Score);
                continue;
            }

            var refused = rightsGate == null ? null : await rightsGate.CheckAsync(resolved, cancellationToken);
            if (refused != null)
            {
                _logger.LogInformation(
                    "Recipe '{Recipe}' is the best semantic candidate (score={Score:F3}) but the user lacks its rights; no other recipe is offered in its place.",
                    resolved.Name, candidate.Score);
                return (null, null, null, refused);
            }

            top = candidate;
            recipe = resolved;
            topIndex = i;
            break;
        }

        if (top == null || recipe == null)
        {
            return (null, null, null, null);
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

            if (string.Equals(candidate.RecipeName, top.RecipeName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var alternativeRecipe = FindRecipeByName(recipes, candidate.RecipeName);
            if (alternativeRecipe != null
                && (IsVetoedByNoneOfGuard(alternativeRecipe, message, _logger, language)
                    || !HasSemanticAnchor(alternativeRecipe, message, _logger, language)))
            {
                continue;
            }

            if (alternativeRecipe != null && rightsGate != null
                && await rightsGate.CheckAsync(alternativeRecipe, cancellationToken) != null)
            {
                continue;
            }

            alternativeGoal = alternativeRecipe?.Goal ?? candidate.RecipeName;
            alternativeGoalTranslations = alternativeRecipe?.GoalTranslations;
            break;
        }

        _logger.LogInformation(
            "Recipe '{Recipe}' matched via semantic fallback (score={Score:F3}, confidence={Confidence}, alternative={Alternative})",
            recipe.Name,
            top.Score,
            top.Score >= SemanticHighConfidenceLogThreshold ? "high" : "grey-zone",
            alternativeGoal ?? "none");

        return (recipe, alternativeGoal, alternativeGoalTranslations, null);
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
            goalTranslations: recipe.GoalTranslations,
            triggerMessage: pending.TriggerMessage);
    }

    public Task<IReadOnlyList<string>> GuaranteedSkillNamesAsync(
        string? userId, string? conversationId, string? message,
        string? language = null, IReadOnlyCollection<string>? userRights = null,
        CancellationToken cancellationToken = default) =>
        GuaranteedSkillNamesAsync(
            userId, conversationId, message, language, userRights, allowSemanticFallback: true, cancellationToken);

    /// <param name="allowSemanticFallback">
    /// False restricts matching to the deterministic trigger walk. The G5 probe passes false: it asks
    /// whether a message routes BY ITSELF, which a similarity score does not answer, and the embedding
    /// round would otherwise be paid on every correction that carries a mutation verb. Full arity and no
    /// default on purpose - an optional parameter appended here would rebind cancellationToken at the
    /// existing call sites without failing to compile (the 2026-09-14 overload trap).
    /// </param>
    public async Task<IReadOnlyList<string>> GuaranteedSkillNamesAsync(
        string? userId, string? conversationId, string? message,
        string? language, IReadOnlyCollection<string>? userRights,
        bool allowSemanticFallback, CancellationToken cancellationToken)
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
                    // Not unconditional: when this very message corrects the paused recipe, the turn will
                    // run a different one, and the toolset is already final by then - assembly happens
                    // before LLMService runs, so the correction branch cannot widen it afterwards.
                    // Guaranteeing the paused recipe's skills in that case is exactly the gap that made a
                    // re-resolved recipe unexecutable.
                    var corrected = await FindCorrectedRecipeAsync(
                        scope, repository, pending, paused, message, language, userRights, cancellationToken);
                    return ExtractStepSkills(corrected ?? paused);
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(message))
        {
            var recipes = await repository.GetAllEnabledAsync(cancellationToken);
            var match = await FindMatchingRecipeAsync(
                scope, recipes, message, language, userRights, cancellationToken, null, allowSemanticFallback);
            if (match.Recipe != null)
            {
                return ExtractStepSkills(match.Recipe);
            }
        }

        return [];
    }

    /// <summary>
    /// Resolves the recipe a correction points at, so the toolset can guarantee ITS step skills. Null when
    /// the message is not a strong correction of the paused recipe, or when nothing else matches - both
    /// leave the caller on the paused recipe's own skills, which is the behaviour before stage 2.
    ///
    /// Matches on the same composite and with the same exclusion the correction branch in LLMService uses,
    /// through RecipeCorrectionComposer. That is not tidiness: FindMatchingRecipeAsync memoizes on
    /// (message, language, excluded), so identical composition means this call warms the entry the branch
    /// then hits - one embedding round instead of two, and the toolset and the plan cannot disagree about
    /// which recipe matched.
    /// </summary>
    private async Task<AgentRecipe?> FindCorrectedRecipeAsync(
        IServiceScope scope,
        IAgentRecipeRepository repository,
        PendingRecipe pending,
        AgentRecipe paused,
        string? message,
        string? language,
        IReadOnlyCollection<string>? userRights,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return null;
        }

        var pausedSteps = Deserialize<List<RecipeStep>>(paused.StepsJson);
        if (pausedSteps == null || pausedSteps.Count == 0)
        {
            return null;
        }

        var pausedPlan = new RecipeExecutionPlan(
            paused.Name,
            pausedSteps,
            new Dictionary<string, string>(pending.Slots, StringComparer.OrdinalIgnoreCase),
            pending.StepIndex,
            captureRewindUsed: pending.CaptureRewindUsed,
            triggerMessage: pending.TriggerMessage);
        if (!RecipeCorrectionDetector.IsStrongCorrection(message, pausedPlan))
        {
            return null;
        }

        var composite = RecipeCorrectionComposer.Compose(pending.TriggerMessage, message);
        var recipes = await repository.GetAllEnabledAsync(cancellationToken);

        // Excluded here rather than filtered afterwards, so the memo entry this writes is the one the
        // correction branch reuses instead of a second entry for the same text.
        var corrected = await FindMatchingRecipeAsync(
            scope, recipes, composite, language, userRights, cancellationToken, paused.Name);
        return corrected.Recipe;
    }

    private static AgentRecipe? FindRecipeByName(List<AgentRecipe> recipes, string? name)
        => recipes.FirstOrDefault(r => string.Equals(r.Name, name, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// The exclusion guard on the semantic fallback path. Carries the pack veto vocabulary as well as
    /// noneOf, because a veto honoured only in the keyword path lets the very same message re-enter the
    /// recipe through embedding ranking - the blind spot fixed on 2026-09-15. The pack side is the union
    /// of every installed language (AllVetoTerms), symmetric to the anchor union in HasSemanticAnchor: the
    /// request language is the UI language, so a Spanish question from a German UI that passes the Spanish
    /// anchor must also meet the Spanish veto. The keyword path (MatchByTrigger) deliberately stays on
    /// the UI-language vetoes.
    /// </summary>
    private static bool IsVetoedByNoneOfGuard(AgentRecipe recipe, string message, ILogger logger, string? language = null)
        => RecipeTriggerMatcher.IsVetoed(
            Deserialize<RecipeTrigger>(recipe.TriggerJson), message, logger, language, recipe.AllVetoTerms());

    /// <summary>
    /// The subject gate on the semantic fallback, shared by the candidate and the alternative. Passes the
    /// recipe's pack anchors of every installed language: the request language is the UI language, so a
    /// Spanish message from a German UI can only be anchored through the Spanish pack.
    /// A trigger marked PackAnchorRequired is anchored by its pack anchors only: its single phrase list gives the
    /// matcher no subject condition, and live 2026-10-20 "Suche selber random die Mitarbeiter" was offered the
    /// writing recipe setup-planning-profile on similarity alone.
    /// </summary>
    private static bool HasSemanticAnchor(AgentRecipe recipe, string message, ILogger logger, string? language)
    {
        var trigger = Deserialize<RecipeTrigger>(recipe.TriggerJson);
        if (trigger?.PackAnchorRequired == true)
        {
            return RecipeTriggerMatcher.HasPackAnchor(recipe.AllAnchors(), message, logger);
        }

        return RecipeTriggerMatcher.HasSemanticAnchor(trigger, message, logger, language, recipe.AllAnchors());
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
            CaptureRewindUsed = plan.CaptureRewindUsed,
            TriggerMessage = RecipeCorrectionComposer.CapForStorage(plan.TriggerMessage)
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
