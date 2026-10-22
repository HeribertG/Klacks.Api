// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Execution-time guard for the rest of a turn in which a call failed with a name-resolution ask-back
/// (NameResolutionAskBack: a by-name resolver found only a suggestion or several matching names). From then on every
/// side-effecting call is rejected without running, whatever its skill: the repeated-write guard is keyed by skill
/// name, so without this guard the model could still write through a different skill - for example by id, after a
/// lenient read resolved the very name the user has not confirmed. Side-effect-free calls
/// (ReadOnlyRecipeWriteGuard.IsSideEffectFree) and preview calls of preview/apply skills (apply not true) still run.
/// </summary>
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Providers;

namespace Klacks.Api.Domain.Services.Assistant;

internal static class NameAskBackWriteGuard
{
    /// <summary>
    /// Marks every side-effecting, non-preview call as rejected once the turn saw a name ask-back and returns the
    /// calls that may execute.
    /// </summary>
    /// <param name="executableCalls">The calls the earlier guards already let through.</param>
    /// <param name="nameAskBackSeen">True when an earlier call of the turn failed with a name ask-back.</param>
    /// <param name="availableFunctions">The turn's toolset, source of each skill's curated effect.</param>
    internal static List<LLMFunctionCall> Reject(
        List<LLMFunctionCall> executableCalls,
        bool nameAskBackSeen,
        IReadOnlyList<LLMFunction>? availableFunctions)
    {
        if (!nameAskBackSeen)
        {
            return executableCalls;
        }

        var allowed = new List<LLMFunctionCall>(executableCalls.Count);
        foreach (var call in executableCalls)
        {
            if (ReadOnlyRecipeWriteGuard.IsSideEffectFree(call, availableFunctions)
                || PreviewApplySkillCalls.IsPreviewCall(call.FunctionName, call.Parameters))
            {
                allowed.Add(call);
                continue;
            }

            call.Success = false;
            call.IsRejectedAfterNameAskBack = true;
            call.Result = LLMLoopConstants.NameAskBackWriteRejectedResult;
        }

        return allowed;
    }
}
