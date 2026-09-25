// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// The closing of a streamed turn after its tool loop: the empty-answer recovery call, the deterministic
/// re-ask of a recipe question the turn answered around, the recipe run bookkeeping and the closing
/// notices. Everything is streamed to the client and appended to the turn's stored text - each piece BEFORE it
/// is offered to the consumer, so the stored answer equals what the user saw even when the consumer walks
/// away. Also hosts the closing guard both chat loops share. A stop request ends the closing before the
/// recovery call, cancels that call while it runs and ends the closing again after it, without touching
/// the recipe run: the turn is then persisted as a stopped one and the recipe stays where it is.
/// </summary>
/// <param name="logger">The chat service's logger, so log categories stay unchanged</param>
/// <param name="functionExecutor">Tells whether the last tool batch ended the turn on a UI passthrough</param>

using System.Runtime.CompilerServices;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Providers;

namespace Klacks.Api.Domain.Services.Assistant;

internal sealed class StreamedTurnClosing
{
    private readonly ILogger _logger;
    private readonly LLMFunctionExecutor _functionExecutor;

    internal StreamedTurnClosing(ILogger logger, LLMFunctionExecutor functionExecutor)
    {
        _logger = logger;
        _functionExecutor = functionExecutor;
    }

    /// <param name="turn">The turn being closed; its text and answered-with-notice flag are completed</param>
    /// <param name="recipe">The turn's recipe bookkeeping</param>
    /// <param name="input">What the closing needs from the tool loop</param>
    /// <param name="cancellationToken">Cancels the recovery call and the recipe writes</param>
    internal async IAsyncEnumerable<SseChunk> StreamAsync(
        TurnRunState turn,
        RecipeTurnState recipe,
        TurnClosingInput input,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (turn.StopRequested)
        {
            yield break;
        }

        var context = turn.Context!;
        var enginePlan = recipe.Plan;
        var recovery = RecoveryFor(
            _logger, input.Provider, turn.Usage, input.Model, input.CurrentMessage, input.SystemPrompt,
            input.VolatilePrompt, input.RunningHistory, input.HistoryBudget, context.Language);
        using var recoveryToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, turn.StopToken);
        await foreach (var recoveryChunk in StreamRecoveryAsync(
                           recovery, turn, input.LastCallStart, recipe.PausedOnAsk, cancellationToken, recoveryToken.Token))
        {
            yield return recoveryChunk;
        }

        if (turn.StopRequested)
        {
            yield break;
        }

        // The turn above ran normally (full toolset) because the user's reply to the pending ask was
        // recognized as an independent question, not a slot answer. The plan is still exactly where it
        // was — same ask step, slot untouched — so once the turn's own answer is done, re-ask it
        // deterministically (RecipeReplyGuard.SafeAsk with no model reply always falls through to the
        // authored translation, no extra model call). A live tool-less re-ask call was tried and reverted:
        // with the ERP explanation still fresh in the running history, the model reliably ignored the
        // "ask the recipe question" instruction and re-explained the just-answered topic instead (reproduced
        // live twice, once even alongside a dropped connection) — a real regression, not a hypothetical
        // one. The deterministic text does not carry the ask step's [REPLIES:...] chips (documented as a
        // known, reported limitation) but is reliable, which this class of bug cannot trade away.
        if (enginePlan != null && enginePlan.TopicSwitchThisTurn && enginePlan.IsActive && enginePlan.CurrentIsAsk)
        {
            var reaskText = RecipeReplyGuard.SafeAsk(
                null, enginePlan.CurrentAskPrompt ?? string.Empty,
                enginePlan.CurrentAskPromptTranslations, context.Language);
            var reaskChunk = RecipeEngineDefaults.TopicSwitchReaskSeparator + reaskText;
            turn.StreamedContent.Append(reaskChunk);
            yield return SseChunk.Content(reaskChunk);
            await recipe.PauseOnReaskAsync(cancellationToken);
        }

        await recipe.FinalizeAsync(cancellationToken);

        foreach (var notice in TurnClosingNotices.Collect(
                     input.IsMutationIntent, recipe.ForceConfirm, turn.StreamedContent.ToString(), turn.Calls,
                     recipe.PausedOnAsk, _logger))
        {
            turn.StreamedContent.Append(notice);
            yield return SseChunk.Content(notice);
        }

        turn.AnsweredWithNotice = recovery.AnsweredWithNotice;
    }

    /// <summary>
    /// Streaming side of the closing guard. Decides from the LAST provider call only - everything streamed
    /// from lastCallStart on - because narration streamed alongside an earlier tool call must not count as
    /// the answer. Text already on screen stays: the recovered answer is appended below it and the same
    /// text is appended to the turn's content, so the stored answer equals what the user saw. The recovery
    /// call is announced with the same calling-model status as every loop call.
    /// </summary>
    /// <param name="recovery">The turn's closing guard.</param>
    /// <param name="turn">The turn; its calls and streamed text are read and the recovered text is added to the latter.</param>
    /// <param name="lastCallStart">Offset in the streamed text where the last provider call's content starts.</param>
    /// <param name="pausedOnRecipeStep">True when a recipe paused on its confirmation or ask step this turn.</param>
    /// <param name="requestToken">The request token; a dropped connection propagates.</param>
    /// <param name="cancellationToken">Cancels the extra call; linked to the turn's stop token, so a stop ends the call and the text of a stopped recovery is neither shown nor stored.</param>
    private async IAsyncEnumerable<SseChunk> StreamRecoveryAsync(
        EmptyAnswerRecovery recovery, TurnRunState turn, int lastCallStart, bool pausedOnRecipeStep,
        CancellationToken requestToken,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var streamedContent = turn.StreamedContent;
        var lastCallContent = streamedContent.ToString(lastCallStart, streamedContent.Length - lastCallStart);
        if (!EmptyAnswerRecovery.NeedsRecovery(
                lastCallContent, turn.Calls, () => _functionExecutor.LastBatchWasUiPassthroughOnly, pausedOnRecipeStep))
        {
            yield break;
        }

        yield return SseChunk.Status(SseStatusStages.CallingModel, LLMService.ElapsedMsFor(turn.Context!), turn.ToolIterations);

        var continuesEarlierContent = !string.IsNullOrWhiteSpace(streamedContent.ToString());
        await using var tokens = recovery.StreamAsync(turn.Calls.Count, continuesEarlierContent, cancellationToken)
            .GetAsyncEnumerator(cancellationToken);
        while (true)
        {
            try
            {
                if (!await tokens.MoveNextAsync())
                {
                    break;
                }
            }
            catch (OperationCanceledException) when (turn.StopRequested && !requestToken.IsCancellationRequested)
            {
                break;
            }

            if (turn.StopRequested)
            {
                break;
            }

            streamedContent.Append(tokens.Current);
            yield return SseChunk.Content(tokens.Current);
        }
    }

    /// <summary>
    /// Closing guard of both loops (EmptyAnswerRecovery). Its one extra call is tool-less and carries the
    /// loop's final message - the last tool results - so it sees exactly what the model last saw. The
    /// running history is fitted to the loop's history budget only when that call is actually built: the
    /// last loop iteration grew it by one more exchange and a function-result message after the loop's
    /// own last fit.
    /// </summary>
    internal static EmptyAnswerRecovery RecoveryFor(
        ILogger logger, ILLMProvider provider, Providers.LLMUsage totalUsage, LLMModel model, string currentMessage,
        string systemPrompt, string? volatilePrompt, List<Providers.LLMMessage> runningHistory, int historyBudget,
        string? language)
    {
        return new EmptyAnswerRecovery(logger, provider, totalUsage, language, instruction =>
        {
            LLMService.FitRunningHistoryToBudget(runningHistory, currentMessage, historyBudget);
            return LLMProviderRequestFactory.Recovery(
                model, currentMessage, systemPrompt, volatilePrompt, instruction, runningHistory);
        });
    }
}
