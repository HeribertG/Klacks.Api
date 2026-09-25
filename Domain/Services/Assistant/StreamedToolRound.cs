// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// The tool half of one iteration of a streamed turn: announces the calls the model made, runs the ones
/// the repeat guard let through, tells the recipe about them, remembers a navigation a call led to and
/// streams every call's result. Whether the round ended the turn is read from <see cref="EndsTurn"/>.
/// One instance covers exactly one round.
/// </summary>
/// <param name="functionExecutor">Runs the calls and reports what kind of calls they were</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Providers;

namespace Klacks.Api.Domain.Services.Assistant;

internal sealed class StreamedToolRound
{
    private readonly LLMFunctionExecutor _functionExecutor;

    internal StreamedToolRound(LLMFunctionExecutor functionExecutor)
    {
        _functionExecutor = functionExecutor;
    }

    /// <summary>
    /// True when the round ran nothing but UI passthrough calls, which ends the turn without asking the
    /// model for prose.
    /// </summary>
    internal bool EndsTurn { get; private set; }

    /// <param name="turn">The turn the round belongs to; receives a navigation the calls led to</param>
    /// <param name="recipe">The turn's recipe bookkeeping, told which calls the model made</param>
    /// <param name="functionCalls">Every call the model made in this round, including rejected ones</param>
    /// <param name="executableCalls">The calls the repeat guard let through</param>
    internal async IAsyncEnumerable<SseChunk> RunAsync(
        TurnRunState turn,
        RecipeTurnState recipe,
        List<Providers.LLMFunctionCall> functionCalls,
        List<Providers.LLMFunctionCall> executableCalls)
    {
        var context = turn.Context!;

        foreach (var call in functionCalls)
        {
            yield return SseChunk.FunctionCallChunk(call.FunctionName, call.Parameters);
        }

        yield return SseChunk.Status(SseStatusStages.ExecutingTool, LLMService.ElapsedMsFor(context), turn.ToolIterations);

        await _functionExecutor.ProcessFunctionCallsAsync(context, executableCalls);
        recipe.Forcing?.Observe(functionCalls);
        if (functionCalls.Any(c => c.RequiresConfirmation))
        {
            recipe.ReleaseOnAutonomyGateHold();
        }

        if (_functionExecutor.NavigationRoute != null)
            turn.NavigationRoute = _functionExecutor.NavigationRoute;
        if (_functionExecutor.NavigationTarget != null)
            turn.NavigationTarget = _functionExecutor.NavigationTarget;

        foreach (var call in functionCalls)
        {
            // Same vacuous-truth guard as EndsTurn below: with an empty execution list
            // HasOnlyUiPassthroughCalls is true although nothing UiPassthrough ran.
            var executionType = executableCalls.Count > 0 && _functionExecutor.HasOnlyUiPassthroughCalls
                ? "UiPassthrough"
                : "Skill";
            yield return SseChunk.FunctionResultChunk(call.FunctionName, call.Result, executionType, call.UiActionSteps, call.UiActionTrackingId);
        }

        // Guarded on executableCalls: with an empty execution list HasOnlyUiPassthroughCalls is
        // vacuously true and would end the turn before the model ever saw the rejection results.
        EndsTurn = executableCalls.Count > 0 && _functionExecutor.HasOnlyUiPassthroughCalls;
    }
}
