// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Persists a SkillSelectionTrajectory record per chat turn, capturing the candidate skills surfaced by
/// the knowledge index and the skill the LLM eventually chose. Privacy-preserving: stores only a short
/// SHA-256 hash plus a 120-char intent excerpt, never the full message. The hash comes from
/// MessageNormalizer, the same source the learning clusters and the correction endpoint use - this class
/// used to hash the raw message while the gap detector hashed a normalised one, so the two could never
/// recognise the same utterance.
/// </summary>
/// <param name="repository">Trajectory repository</param>
/// <param name="caseCollector">Learning collector, fed with the preceding turn once a negation corrects it</param>
/// <param name="phraseRepository">Active learned phrases, matched against the excerpt for fitness attribution</param>
/// <param name="logger">Logger for telemetry warnings</param>

using System.Text.Json;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant;
using Klacks.Api.Domain.Services.Assistant.Providers;

namespace Klacks.Api.Application.Services.Assistant.Evaluation;

public class TrajectoryCaptureService : ITrajectoryCaptureService
{
    private const int ExcerptMaxLength = SkillLearningDefaults.ExcerptMaxLength;
    private const int CandidatesMax = 30;
    private const int OwnerNameMaxLength = 128;

    // Shared with gate G0 of the graceful-correction path: two copies of the same two minutes would let
    // the learning loop and the routing loop disagree about which turn a user just corrected.
    private static readonly TimeSpan ImplicitCorrectionWindow =
        TimeSpan.FromMinutes(GracefulCorrectionDefaults.CorrectionWindowMinutes);

    private readonly ISkillSelectionTrajectoryRepository _repository;
    private readonly ISkillLearningCaseCollector _caseCollector;
    private readonly ISkillPhraseRepository _phraseRepository;
    private readonly ISkillUsageRepository _usageRepository;
    private readonly ILLMRepository _llmRepository;
    private readonly ILogger<TrajectoryCaptureService> _logger;

    public TrajectoryCaptureService(
        ISkillSelectionTrajectoryRepository repository,
        ISkillLearningCaseCollector caseCollector,
        ISkillPhraseRepository phraseRepository,
        ISkillUsageRepository usageRepository,
        ILLMRepository llmRepository,
        ILogger<TrajectoryCaptureService> logger)
    {
        _repository = repository;
        _caseCollector = caseCollector;
        _phraseRepository = phraseRepository;
        _usageRepository = usageRepository;
        _llmRepository = llmRepository;
        _logger = logger;
    }

    public async Task CaptureAsync(
        Guid agentId, LLMContext context, string responseContent, List<LLMFunctionCall> allFunctionCalls,
        string? interruptedPhase = null)
    {
        try
        {
            var interrupted = interruptedPhase != null;

            // An interrupted turn resolves nothing about the turn before it: it never ran to its end, so what
            // its message says about the previous answer is not evidence.
            if (!interrupted && !string.IsNullOrWhiteSpace(context.UserId))
            {
                await ResolvePreviousTurnAsync(agentId, context);
            }

            var llmUsage = await TryGetLlmUsageAsync(context.TurnId);
            var latencyKnowledge = llmUsage?.ToolsetAssemblyMs
                ?? ToIntMs(context.ToolsetAssemblyMs);
            // Total is the sum, not one of the parts: ResponseTimeMs is measured inside LLMService and
            // only starts once the toolset assembly is done, so assembly and response are disjoint
            // intervals and the wait the user actually sits through is both of them. Reporting
            // ResponseTimeMs alone made the total smaller than its own knowledge component. For the
            // same reason nothing is subtracted from the model latency, which stays ResponseTimeMs.
            var latencyLlm = llmUsage?.TtftMs ?? llmUsage?.ResponseTimeMs ?? 0;
            var latencyTotal = latencyKnowledge + (llmUsage?.ResponseTimeMs ?? 0);

            var record = new SkillSelectionTrajectory
            {
                Id = Guid.NewGuid(),
                AgentId = agentId,
                TurnId = context.TurnId,
                UserId = context.UserId,
                Locale = NormalizeLocale(context.Language),
                UserMessageHash = MessageNormalizer.Hash(context.Message),
                IntentExcerpt = MessageNormalizer.Excerpt(context.Message, ExcerptMaxLength),
                KnowledgeIndexCandidatesJson = SerializeCandidates(context.AvailableFunctions),
                LlmChosenSkill = allFunctionCalls.FirstOrDefault()?.FunctionName,
                WasExecuted = allFunctionCalls.Count > 0,
                WasSuccessful = interrupted ? null : await ComputeWasSuccessfulAsync(context.TurnId),
                HadMutationIntent = MutationIntentDetector.IsMutationIntent(context.Message),
                WasCorrected = false,
                CorrectionType = CorrectionTypes.None,
                LatencyMsTotal = latencyTotal,
                LatencyMsKnowledge = latencyKnowledge,
                LatencyMsLlm = latencyLlm,
                RecipeName = Truncate(context.ActiveRecipeName),
                RecipeOutcome = interrupted ? null : ResolveRecipeOutcome(context),
                WasInterrupted = interrupted,
                InterruptedPhase = interruptedPhase,
                LearnedPhraseHit = await FindLearnedPhraseHitAsync(context.Message),
                CreateTime = DateTime.UtcNow
            };

            await _repository.AddAsync(record);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Trajectory capture failed for agent {AgentId}", agentId);
        }
    }

    private async Task<Klacks.Api.Domain.Models.Assistant.LLMUsage?> TryGetLlmUsageAsync(Guid? turnId)
    {
        if (!turnId.HasValue)
        {
            return null;
        }

        try
        {
            // The usage row is written (and awaited) before the background capture starts, so it is
            // normally visible here. A failure must not cost the trajectory row itself.
            return await _llmRepository.GetUsageByIdAsync(turnId.Value);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Reading llm_usage for trajectory latencies failed for turn {TurnId}", turnId);
            return null;
        }
    }

    private static int ToIntMs(long? value) =>
        value.HasValue ? (int)Math.Min(value.Value, int.MaxValue) : 0;

    private async Task<bool?> ComputeWasSuccessfulAsync(Guid? turnId)
    {
        if (!turnId.HasValue)
        {
            return null;
        }

        var usageRows = await _usageRepository.GetByTurnIdAsync(turnId.Value);
        if (usageRows.Count == 0)
        {
            return null;
        }

        // W1.4: a dispatched UiAction is not a verdict yet - the browser reports Completed/Failed
        // later, after this capture has already run. Rows still in Dispatched state are therefore
        // excluded from the verdict, and so are rows the user's stop kept from running (Cancelled);
        // a turn that consists only of such rows stays unknown instead of being booked as a false success.
        var decisiveRows = usageRows
            .Where(SkillUsagePredicates.RowHasVerdict)
            .ToList();

        if (decisiveRows.Count == 0)
        {
            return null;
        }

        return decisiveRows.All(row => row.Success);
    }

    // A bare negation is its own admission ticket to the lookup, next to the correction signal and the
    // resumed recipe. The correction vocabulary is the smaller of the two sets ("nein, nicht, falsch, no,
    // not, wrong, non, faux ..."), so "Nö", "Nee", "Nope", "Rien" and every plugin-language refusal used
    // to miss the gate entirely and leave the confirmation outcome pending for good.
    private async Task ResolvePreviousTurnAsync(Guid agentId, LLMContext context)
    {
        var isCorrectionSignal = ImplicitCorrectionDetector.IsCorrectionSignal(context.Message);
        var isBareNegation = DeclineDetector.IsBareNegation(context.Message);
        var confirmedGate = context.RecipeConfirmationAccepted;
        var abandonedGate = context.RecipeConfirmationDeclined;
        var resumesRecipe = !context.RecipeAwaitingConfirmation
            && !string.IsNullOrWhiteSpace(context.ActiveRecipeName);

        if (!isCorrectionSignal && !isBareNegation && !resumesRecipe && !confirmedGate && !abandonedGate)
        {
            return;
        }

        var previous = await _repository.FindMostRecentByAgentAndUserAsync(agentId, context.UserId);
        if (previous == null || previous.WasCorrected)
        {
            return;
        }

        if (previous.WasInterrupted)
        {
            await ResolveInterruptedPreviousAsync(agentId, previous, context, isCorrectionSignal);
            return;
        }

        // Before the window, and without asking the message anything: the engine itself cleared the gate
        // this turn, so nothing has to be attributed by heuristic. The window bounds an attribution, not
        // a fact, and the two clocks disagree - a user who reads the confirmation question and answers
        // after two minutes still resumes the recipe (the pending store keeps it for
        // RecipeEngineDefaults.PendingRecipeTtlMinutes), and the gate used to stay pending for good.
        if (confirmedGate && string.Equals(previous.RecipeOutcome, RecipeOutcomes.Pending, StringComparison.Ordinal))
        {
            await MarkRecipeOutcomeAsync(previous, RecipeOutcomes.Confirmed);
            return;
        }

        // Same reasoning as the confirmed gate one branch up, for its mirror image: the engine itself
        // abandoned the gate this turn, so the outcome is a fact and needs neither the window nor a
        // second reading of the message. What the message still decides is WHICH outcome - a plain
        // refusal is a verdict on the trigger and keeps feeding the decline signal, while a reply that
        // redirects the conversation says nothing about the recipe and must stay out of it, or every
        // "nein, zeig mir stattdessen die Kunden" would teach the cluster policy that the trigger was
        // wrong. Without this branch such a turn left the gate pending for good.
        if (abandonedGate && string.Equals(previous.RecipeOutcome, RecipeOutcomes.Pending, StringComparison.Ordinal))
        {
            if (isBareNegation)
            {
                await MarkRecipeDeclinedAsync(agentId, previous);
                return;
            }

            await MarkRecipeOutcomeAsync(previous, RecipeOutcomes.Redirected);
            return;
        }

        if (DateTime.UtcNow - previous.CreateTime > ImplicitCorrectionWindow)
        {
            return;
        }

        if (string.Equals(previous.RecipeOutcome, RecipeOutcomes.Pending, StringComparison.Ordinal))
        {
            await ResolvePendingRecipeAsync(agentId, previous, context, isBareNegation);
            return;
        }

        if (!isCorrectionSignal || string.IsNullOrWhiteSpace(previous.LlmChosenSkill))
        {
            return;
        }

        await MarkImplicitCorrectionAsync(agentId, previous, context.GracefulCorrectionApplied);
    }

    // A turn the user stopped counts for nothing, with one exception: when the next message is a correction
    // that the graceful-correction path really re-routed, the stop and the correction together are the
    // strongest signal there is that the first routing was wrong, and it is booked as such. Nothing else
    // about an interrupted turn is resolved by what follows it - not an implicit correction, not a recipe
    // gate - because the turn never reached the point where the user could have judged it.
    private async Task ResolveInterruptedPreviousAsync(
        Guid agentId, SkillSelectionTrajectory previous, LLMContext context, bool isCorrectionSignal)
    {
        if (!isCorrectionSignal
            || !context.GracefulCorrectionApplied
            || string.IsNullOrWhiteSpace(previous.LlmChosenSkill)
            || DateTime.UtcNow - previous.CreateTime > ImplicitCorrectionWindow)
        {
            return;
        }

        await MarkImplicitCorrectionAsync(agentId, previous, wasRerouted: true);
    }

    // A bare negation answers the assistant's own question and says the recipe trigger was too broad; a
    // negation that carries content is an ordinary turn and resolves nothing. The confirmed branch needs
    // both halves of the evidence: an affirmation AND the same recipe running again. The affirmation alone
    // is what LLMService acts on to clear the gate, and without it the same recipe re-triggering from a
    // rejection ("Nein, neue Gruppe anlegen" discards the pending recipe and is matched afresh) would book
    // a gate as confirmed that LLMService had just recorded as declined. The stored name went through
    // Truncate, the name on the context did not, so the comparison has to truncate too. This inference
    // is the fallback only: a turn that carries one of the engine's own gate flags
    // (RecipeConfirmationAccepted, RecipeConfirmationDeclined) was resolved by the caller before the
    // window check and never reaches it. What still arrives here is a turn the engine never saw as a
    // gate at all - a gated turn whose successor went down another chat path entirely.
    private async Task ResolvePendingRecipeAsync(
        Guid agentId, SkillSelectionTrajectory previous, LLMContext context, bool isBareNegation)
    {
        if (isBareNegation)
        {
            await MarkRecipeDeclinedAsync(agentId, previous);
            return;
        }

        if (AffirmationDetector.IsAffirmation(context.Message)
            && string.Equals(previous.RecipeName, Truncate(context.ActiveRecipeName), StringComparison.Ordinal))
        {
            await MarkRecipeOutcomeAsync(previous, RecipeOutcomes.Confirmed);
        }
    }

    /// <summary>
    /// Books a refused gate as declined AND feeds the decline learning signal, which is the one pairing
    /// that must never come apart: the outcome without the case makes the refusal invisible to
    /// RecipeDeclineClusterPolicy, and the case without the outcome lets a later turn resolve the same
    /// gate a second time. The two entry points reach it from different evidence - the engine's own flag
    /// and the window-bounded inference - so it lives here rather than in either of them.
    /// </summary>
    /// <param name="agentId">Agent the decline is clustered under.</param>
    /// <param name="previous">The trajectory of the turn that asked the confirmation question.</param>
    private async Task MarkRecipeDeclinedAsync(Guid agentId, SkillSelectionTrajectory previous)
    {
        await MarkRecipeOutcomeAsync(previous, RecipeOutcomes.Declined);

        await _caseCollector.CollectRecipeDeclineAsync(new SkillLearningRecipeDecline(
            agentId,
            previous.UserMessageHash,
            previous.IntentExcerpt,
            previous.UserId,
            previous.Locale,
            previous.RecipeName,
            previous.KnowledgeIndexCandidatesJson,
            previous.Id));
    }

    private async Task MarkRecipeOutcomeAsync(SkillSelectionTrajectory previous, string outcome)
    {
        previous.RecipeOutcome = outcome;
        previous.UpdateTime = DateTime.UtcNow;
        await _repository.UpdateAsync(previous);
    }

    // The WasCorrected guard in the caller is what keeps this to one case per corrected turn. The cluster
    // key is the stored hash of the preceding message, never a hash of its excerpt: for anything longer
    // than the excerpt limit the two differ and would split one wish across two clusters.
    private async Task MarkImplicitCorrectionAsync(
        Guid agentId, SkillSelectionTrajectory previous, bool wasRerouted)
    {
        previous.WasCorrected = true;
        previous.CorrectionType = wasRerouted ? CorrectionTypes.GracefulRerouted : CorrectionTypes.Implicit;
        previous.UpdateTime = DateTime.UtcNow;
        await _repository.UpdateAsync(previous);

        await _caseCollector.CollectImplicitCorrectionAsync(new SkillLearningImplicitCorrection(
            agentId,
            previous.UserMessageHash,
            previous.IntentExcerpt,
            previous.UserId,
            previous.Locale,
            previous.LlmChosenSkill,
            previous.KnowledgeIndexCandidatesJson,
            previous.Id));
    }

    // Attribution for the usefulness quote of a learned phrase, and deliberately nothing more than a
    // substring test: it says the wording occurred, not that it caused the routing. Evaluated now rather
    // than when the fitness service runs, so a phrase learned next week cannot claim credit for a turn
    // that happened today. Costs one read of the learned phrases per turn on a fire-and-forget path;
    // while nothing has been learned yet that read returns an empty list.
    private async Task<string?> FindLearnedPhraseHitAsync(string? message)
    {
        var learned = await _phraseRepository.GetActiveBySourceAsync(
            SkillPhraseSources.Learned, LearnedPhraseMatcher.MatchLimit);

        return Truncate(LearnedPhraseMatcher.FirstMatchingOwner(learned, message));
    }

    private static string? ResolveRecipeOutcome(LLMContext context) =>
        context.RecipeAwaitingConfirmation && !string.IsNullOrWhiteSpace(context.ActiveRecipeName)
            ? RecipeOutcomes.Pending
            : null;

    private static string? Truncate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= OwnerNameMaxLength ? trimmed : trimmed[..OwnerNameMaxLength];
    }

    private static string NormalizeLocale(string? language)
    {
        if (string.IsNullOrWhiteSpace(language)) return "??";
        var trimmed = language.Trim();
        return trimmed.Length <= 8 ? trimmed : trimmed[..8];
    }

    // W1.6: each candidate carries its provenance (why it was in the toolset) plus the 1-based rank
    // in the offered list and the retrieval score where one exists. This turns "which source won" into
    // a SQL query over the jsonb column instead of a guess.
    private static string SerializeCandidates(List<LLMFunction>? functions)
    {
        if (functions == null || functions.Count == 0) return "[]";
        var trimmed = functions.Count > CandidatesMax ? functions.GetRange(0, CandidatesMax) : functions;
        var payload = trimmed.Select((f, index) => new
        {
            name = f.Name,
            source = f.ToolsetSource?.ToString(),
            rank = index + 1,
            score = f.RetrievalScore
        });
        return JsonSerializer.Serialize(payload);
    }
}
