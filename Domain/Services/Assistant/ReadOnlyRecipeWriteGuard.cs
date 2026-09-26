// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Execution-time guard for the reply that follows a read-only recipe (only ask/search steps): once such a
/// recipe executed its final step in the current turn, every side-effecting call of the rest of the turn is
/// rejected without running. Live 2026-09-26 (period-close-schedule): after the forced search the model took
/// the user's slot answer ("5 Tage") as an order to store and called the Sensitive store skill in the same
/// turn, which left a waiting confirmation token behind - the confirm_pending_action trap. Filtering the
/// offered toolset would not be a guarantee, because LLMFunctionExecutor runs any enabled skill by name, and
/// it would change the tool array mid-turn (prompt-prefix cache). Read-only skills and navigation stay
/// allowed (same predicate as RepeatedWriteCallGuard); confirm_pending_action is side-effecting and rejected.
/// </summary>
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Services.Assistant.Providers;

namespace Klacks.Api.Domain.Services.Assistant;

internal static class ReadOnlyRecipeWriteGuard
{
    /// <summary>
    /// Marks every side-effecting call as rejected and returns the calls that may execute.
    /// </summary>
    /// <param name="executableCalls">The calls the repeat guard already let through.</param>
    /// <param name="readOnlyRecipeCompleted">True when a read-only recipe executed its final step this turn.</param>
    internal static List<LLMFunctionCall> Reject(List<LLMFunctionCall> executableCalls, bool readOnlyRecipeCompleted)
    {
        if (!readOnlyRecipeCompleted)
        {
            return executableCalls;
        }

        var allowed = new List<LLMFunctionCall>(executableCalls.Count);
        foreach (var call in executableCalls)
        {
            if (RepeatedWriteCallGuard.IsRepeatable(call))
            {
                allowed.Add(call);
                continue;
            }

            call.Success = false;
            call.Result = LLMLoopConstants.ReadOnlyRecipeWriteRejectedResult;
        }

        return allowed;
    }
}
