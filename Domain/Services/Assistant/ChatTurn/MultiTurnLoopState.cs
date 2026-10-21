// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The mutable state of one non-streaming chat turn's tool loop, carried from iteration to iteration and
/// into the turn's closing. One instance covers exactly one turn.
/// </summary>
/// <param name="runningHistory">The history the loop starts from and grows</param>
/// <param name="currentMessage">The message the first iteration sends, the user's message</param>
/// <param name="historyBudget">Token budget the running history is fitted to before every call</param>
/// <param name="isMutationIntent">True when the user's message asks for a change</param>
/// <param name="isNavigationIntent">True when the user's message asks to go somewhere</param>
/// <param name="recipe">The turn's recipe bookkeeping</param>

using Klacks.Api.Domain.Services.Assistant.Providers;

namespace Klacks.Api.Domain.Services.Assistant.ChatTurn;

internal sealed class MultiTurnLoopState
{
    internal MultiTurnLoopState(
        List<Providers.LLMMessage> runningHistory,
        string currentMessage,
        int historyBudget,
        bool isMutationIntent,
        bool isNavigationIntent,
        RecipeTurnState recipe)
    {
        RunningHistory = runningHistory;
        CurrentMessage = currentMessage;
        HistoryBudget = historyBudget;
        IsMutationIntent = isMutationIntent;
        IsNavigationIntent = isNavigationIntent;
        Recipe = recipe;
        EnginePlan = recipe.Plan;
    }

    internal List<LLMFunctionCall> AllFunctionCalls { get; } = new();

    internal List<Providers.LLMMessage> RunningHistory { get; }

    internal string CurrentMessage { get; set; }

    internal int HistoryBudget { get; }

    internal string ResponseContent { get; set; } = string.Empty;

    internal LLMProviderResponse? LastResponse { get; set; }

    internal int IterationsUsed { get; set; }

    internal HashSet<string> CalledFunctionNames { get; } = new(StringComparer.OrdinalIgnoreCase);

    internal bool IsMutationIntent { get; }

    internal bool IsNavigationIntent { get; }

    internal RecipeTurnState Recipe { get; }

    internal RecipeExecutionPlan? EnginePlan { get; }

    internal bool ForcedRetryUsed { get; set; }
}
