// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// The recipe step at the top of a streamed tool-loop iteration: when the active recipe plan waits on the
/// user's confirmation or on an ask step, the model is asked for that one question, its answer is streamed
/// and the recipe is paused for the next turn. Yields nothing when no plan is waiting. The turn ends on the
/// pause, which the caller reads from RecipeTurnState.PausedOnAsk. A stop request ends the step before the
/// question is put, or cancels it while it runs; the recipe is then left exactly where it was.
/// </summary>

using System.Runtime.CompilerServices;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Providers;

namespace Klacks.Api.Domain.Services.Assistant;

internal static class RecipePauseStep
{
    /// <param name="logger">The chat service's logger, so log categories stay unchanged</param>
    /// <param name="recipe">The turn's recipe bookkeeping; its plan decides whether a question is due</param>
    /// <param name="provider">The provider the recipe question is put to</param>
    /// <param name="model">The model the recipe question is put to</param>
    /// <param name="currentMessage">The message of the current loop iteration</param>
    /// <param name="systemPrompt">The turn's stable system prompt</param>
    /// <param name="volatilePrompt">The turn's volatile system prompt</param>
    /// <param name="runningHistory">The history the loop has built up so far</param>
    /// <param name="turn">The turn; receives the usage and the streamed text</param>
    /// <param name="cancellationToken">The request token; the turn's stop token is linked to it for the model call</param>
    internal static async IAsyncEnumerable<SseChunk> StreamAsync(
        ILogger logger,
        RecipeTurnState recipe,
        ILLMProvider provider,
        LLMModel model,
        string currentMessage,
        string systemPrompt,
        string? volatilePrompt,
        List<Providers.LLMMessage> runningHistory,
        TurnRunState turn,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var enginePlan = recipe.Plan;
        var context = turn.Context!;

        enginePlan?.AdvanceOverSatisfied();
        if (enginePlan != null && enginePlan.NeedsConfirmation)
        {
            var confirmInstruction = enginePlan.ConfirmationInstruction;
            yield return SseChunk.Status(SseStatusStages.CallingModel, LLMService.ElapsedMsFor(context), turn.ToolIterations);
            if (turn.StopRequested)
            {
                yield break;
            }

            var confirmRequest = LLMProviderRequestFactory.RecipeStep(
                model, currentMessage, systemPrompt, volatilePrompt, confirmInstruction, runningHistory);
            var confirmResponse = await StopAwareCall.RunAsync(
                turn, cancellationToken, token => TransientProviderRetry.ProcessAsync(provider, confirmRequest, logger, token));
            if (confirmResponse == null || (!confirmResponse.Success && turn.StopRequested))
            {
                yield break;
            }

            cancellationToken.ThrowIfCancellationRequested();

            LLMUsageAccumulator.Add(turn.Usage, confirmResponse.Usage);
            var confirmText = RecipeReplyGuard.WithConfirmationChip(RecipeReplyGuard.SafeConfirmation(
                confirmResponse.Success ? confirmResponse.Content : null, enginePlan.Goal,
                enginePlan.AlternativeGoal, context.Language, enginePlan.GoalTranslations,
                enginePlan.AlternativeGoalTranslations), enginePlan.AlternativeGoal, context.Language);
            turn.StreamedContent.Append(confirmText);
            yield return SseChunk.Content(confirmText);
            await recipe.PauseForConfirmationAsync(cancellationToken);
        }
        else if (enginePlan != null && enginePlan.IsActive && enginePlan.CurrentIsAsk && !enginePlan.TopicSwitchThisTurn)
        {
            var askInstruction = string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                RecipeEngineDefaults.AskStepInstructionTemplate, enginePlan.CurrentAskPrompt);
            yield return SseChunk.Status(SseStatusStages.CallingModel, LLMService.ElapsedMsFor(context), turn.ToolIterations);
            if (turn.StopRequested)
            {
                yield break;
            }

            var askRequest = LLMProviderRequestFactory.RecipeStep(
                model, currentMessage, systemPrompt, volatilePrompt, askInstruction, runningHistory);
            var askResponse = await StopAwareCall.RunAsync(
                turn, cancellationToken, token => TransientProviderRetry.ProcessAsync(provider, askRequest, logger, token));
            if (askResponse == null || (!askResponse.Success && turn.StopRequested))
            {
                yield break;
            }

            cancellationToken.ThrowIfCancellationRequested();

            LLMUsageAccumulator.Add(turn.Usage, askResponse.Usage);
            var askText = RecipeReplyGuard.SafeAsk(
                askResponse.Success ? askResponse.Content : null, enginePlan.CurrentAskPrompt ?? string.Empty,
                enginePlan.CurrentAskPromptTranslations, context.Language);
            turn.StreamedContent.Append(askText);
            yield return SseChunk.Content(askText);
            await recipe.PauseOnAskAsync(cancellationToken);
        }
    }
}
