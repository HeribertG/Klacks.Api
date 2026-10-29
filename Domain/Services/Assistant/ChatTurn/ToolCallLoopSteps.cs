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

    /// <summary>
    /// Recipe forcing data flow (shared by both loops): deterministically inject captured values (the
    /// resolved clientId from find_customer_candidates) into the next forced step's parameters before it
    /// executes — reliable in-code data flow between steps, not a fragile model-carries-the-id hop.
    /// An enum parameter is injected only as an allowed value; RecipeEnumInjectionResolver decides when the
    /// model's own value stays instead of the user's raw answer.
    /// </summary>
    /// <param name="recipePlan">The forcing plan of the turn, or null when no recipe is forced</param>
    /// <param name="functionCalls">The calls the model made in this iteration</param>
    /// <param name="availableFunctions">The turn's toolset, read for the enum values the forced skill declares</param>
    /// <param name="logger">The chat service's logger</param>
    internal static void ApplyRecipeInjections(
        IRecipeForcingPlan? recipePlan,
        IEnumerable<LLMFunctionCall> functionCalls,
        IReadOnlyList<LLMFunction> availableFunctions,
        ILogger logger)
    {
        if (recipePlan == null)
        {
            return;
        }

        foreach (var call in functionCalls)
        {
            var function = availableFunctions.FirstOrDefault(f =>
                string.Equals(f.Name, call.FunctionName, StringComparison.OrdinalIgnoreCase));
            foreach (var injection in recipePlan.GetParameterInjections(call.FunctionName))
            {
                call.Parameters.TryGetValue(injection.Key, out var modelValue);
                var value = RecipeEnumInjectionResolver.Resolve(function, injection.Key, injection.Value, modelValue);
                if (value == null)
                {
                    logger.LogInformation(
                        "Recipe injection into {Skill}.{Parameter} skipped: the slot value is not an allowed enum value, " +
                        "the model's own value is kept", call.FunctionName, injection.Key);
                    continue;
                }

                call.Parameters[injection.Key] = value;
            }
        }
    }
}
