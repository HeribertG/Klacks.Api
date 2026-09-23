// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Execution-time replacement for the former per-iteration toolset shrinking: read-only skills, navigation
/// and catalogued read-only actions of multi-action skills (ReadOnlySkillActions) may repeat freely, while
/// a side-effecting call whose skill already ran in an EARLIER iteration must not run twice in one turn
/// (multiple calls within the same batch stay allowed). Rejected calls keep flowing through the result
/// pipeline with an instructive message so the model corrects itself on the next iteration. A
/// recipe-forced iteration is exempt: the forcing spine may deliberately re-run a step skill and its calls
/// are narrowed deterministically, not chosen by the model. Only side-effecting calls are recorded, so a
/// read-only action never blocks a later write action of the same skill, while a repeated write action is
/// still rejected on its second and every further attempt.
/// </summary>
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Services.Assistant.Providers;

namespace Klacks.Api.Domain.Services.Assistant;

internal static class RepeatedWriteCallGuard
{
    /// <summary>
    /// True when the call may run any number of times in one turn.
    /// </summary>
    /// <param name="call">The model's function call, including its arguments.</param>
    internal static bool IsRepeatable(LLMFunctionCall call) =>
        ReadOnlySkillPrefixes.HasReadOnlyPrefix(call.FunctionName)
        || string.Equals(call.FunctionName, SkillNames.NavigateTo, StringComparison.OrdinalIgnoreCase)
        || ReadOnlySkillActions.IsReadOnlyCall(call.FunctionName, call.Parameters);

    /// <summary>
    /// Marks every non-repeatable call whose skill already ran in an earlier iteration as a rejected repeat
    /// and returns the calls that may execute.
    /// </summary>
    /// <param name="functionCalls">The calls of the current iteration.</param>
    /// <param name="previouslyCalledNames">Side-effecting skills that already ran earlier in this turn.</param>
    /// <param name="forceRecipe">True for a recipe-forced iteration, which is exempt from the rule.</param>
    internal static List<LLMFunctionCall> Reject(
        List<LLMFunctionCall> functionCalls,
        HashSet<string> previouslyCalledNames,
        bool forceRecipe)
    {
        if (forceRecipe || previouslyCalledNames.Count == 0)
        {
            return functionCalls;
        }

        var executable = new List<LLMFunctionCall>(functionCalls.Count);
        foreach (var call in functionCalls)
        {
            if (!IsRepeatable(call) && previouslyCalledNames.Contains(call.FunctionName))
            {
                call.Success = false;
                call.IsRejectedRepeat = true;
                call.Result = LLMLoopConstants.RepeatedWriteCallRejectedResult;
            }
            else
            {
                executable.Add(call);
            }
        }

        return executable;
    }

    /// <summary>
    /// Records the side-effecting skills of this iteration so later iterations reject their repeats.
    /// </summary>
    /// <param name="functionCalls">The calls of the current iteration.</param>
    /// <param name="calledNames">The turn's set of side-effecting skills that already ran.</param>
    internal static void Record(IEnumerable<LLMFunctionCall> functionCalls, HashSet<string> calledNames)
    {
        foreach (var call in functionCalls.Where(call => !IsRepeatable(call)))
        {
            calledNames.Add(call.FunctionName);
        }
    }

    /// <summary>
    /// Rejects this iteration's repeats against the earlier iterations, then records the iteration.
    /// </summary>
    /// <param name="functionCalls">The calls of the current iteration.</param>
    /// <param name="calledNames">The turn's set of side-effecting skills that already ran.</param>
    /// <param name="forceRecipe">True for a recipe-forced iteration, which is exempt from the rule.</param>
    internal static List<LLMFunctionCall> RejectAndRecord(
        List<LLMFunctionCall> functionCalls,
        HashSet<string> calledNames,
        bool forceRecipe)
    {
        var executable = Reject(functionCalls, calledNames, forceRecipe);
        Record(functionCalls, calledNames);
        return executable;
    }
}
