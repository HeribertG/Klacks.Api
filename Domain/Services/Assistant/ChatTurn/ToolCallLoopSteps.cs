// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The tool-call steps both chat loops share between a model answer and the next iteration: keeping calls
/// to the turn's toolset, injecting the values a forced recipe captured into the next step's parameters, and
/// formatting the results into the follow-up message.
/// </summary>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Providers;

namespace Klacks.Api.Domain.Services.Assistant.ChatTurn;

internal static class ToolCallLoopSteps
{
    /// <param name="functionCalls">The calls the model made in this iteration</param>
    /// <param name="context">The turn context carrying the assembled toolset</param>
    /// <param name="logger">The chat service's logger, so log categories stay unchanged</param>
    internal static List<LLMFunctionCall> InToolset(List<LLMFunctionCall> functionCalls, LLMContext context, ILogger logger)
    {
        return ToolsetMembershipGuard.Apply(
            functionCalls, context, LLMLoopConstants.DefaultToolsetMembershipMode, logger);
    }

    // Prompt-injection containment lives in ToolResultFormatter, shared with the turn replay and the
    // read-only research sub-loop. Internal (not private): covered by LLMServiceFormatFunctionResultsTests.
    internal static string FormatFunctionResults(List<LLMFunctionCall> functionCalls, int? maxToolResultChars = null)
    {
        return ToolResultFormatter.Format(functionCalls, maxToolResultChars ?? LLMLoopConstants.DefaultMaxToolResultChars);
    }

    // Recipe forcing data flow (shared by both loops): deterministically inject captured values (the
    // resolved clientId from find_customer_candidates) into the next forced step's parameters before it
    // executes — reliable in-code data flow between steps, not a fragile model-carries-the-id hop.
    internal static void ApplyRecipeInjections(IRecipeForcingPlan? recipePlan, IEnumerable<LLMFunctionCall> functionCalls)
    {
        if (recipePlan == null)
        {
            return;
        }

        foreach (var call in functionCalls)
        {
            foreach (var injection in recipePlan.GetParameterInjections(call.FunctionName))
            {
                call.Parameters[injection.Key] = injection.Value;
            }
        }
    }
}
