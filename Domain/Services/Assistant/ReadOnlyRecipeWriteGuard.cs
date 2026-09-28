// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Execution-time guard for the reply that follows a completed recipe: once a recipe executed its final step in
/// the current turn, every side-effecting call of the rest of the turn is rejected without running. Live
/// 2026-09-26 (period-close-schedule, read-only): after the forced search the model took the user's slot answer
/// ("5 Tage") as an order to store and called the Sensitive store skill in the same turn, which left a waiting
/// confirmation token behind - the confirm_pending_action trap. Since the final step's note of a WRITING recipe
/// also reaches the reply call (RecipeEngineDefaults.WritingRecipeCompletedNotePrefix), the guard covers those
/// too: several final notes name a follow-up write that must wait for the user (delete_break after a confirmation,
/// the apply step after a preview, set_planning_profile_parameters), and RepeatedWriteCallGuard only stops the step
/// skill itself. Filtering the offered toolset would not be a guarantee, because LLMFunctionExecutor runs any
/// enabled skill by name, and it would change the tool array mid-turn (prompt-prefix cache).
///
/// What still runs (IsSideEffectFree): navigation, the catalogued read-only actions of multi-action skills
/// (ReadOnlySkillActions), and every skill whose curated effect (AgentSkill.Effect, copied onto the turn's
/// LLMFunction by the toolset assembler) is Explain, Read or Advise - so explain_* skills and read-only UI skills
/// (open_order_export, select_group, search_in_list) are no longer rejected just because their names carry no read
/// prefix. The effect is the right signal here and SkillRiskClassifier's ReadOnly class is not: the classifier
/// deliberately lets draft and proposal steps run ungated (create_plan, start_company_rule, start_guided_tour ...),
/// and create_plan issues exactly the confirmation token this guard exists to prevent; all of those carry the
/// effect Mutate and stay rejected. A guard test pins that every non-Mutate seed skill also classifies ReadOnly, so
/// the effect can never let through a skill the classifier would gate. The effect is fail-closed (a missing or
/// unknown value is Mutate). Only a call without a known effect - its skill is not in the turn's toolset, or the
/// function was not built by the toolset assembler - falls back to the read-only name prefix of
/// RepeatedWriteCallGuard, which is the guard's previous behaviour.
/// </summary>
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Providers;

namespace Klacks.Api.Domain.Services.Assistant;

internal static class ReadOnlyRecipeWriteGuard
{
    /// <summary>
    /// Marks every side-effecting call as rejected and returns the calls that may execute.
    /// </summary>
    /// <param name="executableCalls">The calls the repeat guard already let through.</param>
    /// <param name="recipeCompleted">True when a recipe executed its final step this turn.</param>
    /// <param name="rejectedResult">The tool result a rejected call carries (differs for read-only and writing recipes).</param>
    /// <param name="availableFunctions">The turn's toolset, source of each skill's curated effect.</param>
    internal static List<LLMFunctionCall> Reject(
        List<LLMFunctionCall> executableCalls,
        bool recipeCompleted,
        string rejectedResult,
        IReadOnlyList<LLMFunction>? availableFunctions)
    {
        if (!recipeCompleted)
        {
            return executableCalls;
        }

        var allowed = new List<LLMFunctionCall>(executableCalls.Count);
        foreach (var call in executableCalls)
        {
            if (IsSideEffectFree(call, availableFunctions))
            {
                allowed.Add(call);
                continue;
            }

            call.Success = false;
            call.Result = rejectedResult;
        }

        return allowed;
    }

    /// <summary>
    /// True when the call cannot store, change or confirm anything; see the class summary for the order of checks.
    /// </summary>
    /// <param name="call">The model's function call, including its arguments.</param>
    /// <param name="availableFunctions">The turn's toolset, source of each skill's curated effect.</param>
    internal static bool IsSideEffectFree(LLMFunctionCall call, IReadOnlyList<LLMFunction>? availableFunctions)
    {
        if (string.Equals(call.FunctionName, SkillNames.NavigateTo, StringComparison.OrdinalIgnoreCase)
            || ReadOnlySkillActions.IsReadOnlyCall(call.FunctionName, call.Parameters))
        {
            return true;
        }

        var function = availableFunctions?.FirstOrDefault(
            candidate => string.Equals(candidate.Name, call.FunctionName, StringComparison.OrdinalIgnoreCase));
        if (function?.Effect is { } effect)
        {
            return effect != SkillEffect.Mutate;
        }

        return RepeatedWriteCallGuard.IsRepeatable(call);
    }
}
