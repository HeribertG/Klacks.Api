// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Execution-time check that a skill the model calls is part of the toolset the turn was given. The function
/// executor runs any enabled skill by name, so without this check a name taken from the conversation history,
/// from a tool result or from an injected instruction would run although the turn never offered it. It is judged
/// against the turn's FULL toolset (LLMContext.AvailableFunctions), never against the narrowed set of one
/// iteration: a recipe-forced or confirmation-forced iteration only ever narrows to members of that toolset, so
/// every legitimate call is covered. A name is resolved through SkillNameAliases first (create_client is
/// create_employee), and confirm_pending_action always belongs because it redeems a held action whatever the
/// toolset. A turn without a known toolset is not judged, because there is nothing to compare against.
/// In Observe mode an outside call is only flagged (LLMFunctionCall.IsOutsideToolset) and logged, and still runs.
/// In Enforce mode it is also rejected with an instructive result and returned to the model like a rejected
/// repeat (IsRejectedOutsideToolset), and does not run. MCP and the REST execute-function endpoint do not pass
/// through the chat loop and are unaffected.
/// Every outside call is logged as one structured warning (TurnId, ConversationId, ModelId, FunctionName, Mode,
/// ToolsetSize). In Observe mode the function executor logs the outcome of the same call afterwards, so the rate of
/// outside calls that really succeeded - the calls Enforce would have blocked - is countable from the two lines.
/// </summary>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Providers;

namespace Klacks.Api.Domain.Services.Assistant;

internal static class ToolsetMembershipGuard
{
    /// <summary>
    /// Flags every call to a skill outside the toolset and returns the calls that may execute.
    /// </summary>
    /// <param name="functionCalls">The calls of the current iteration, in the order the model made them.</param>
    /// <param name="context">The turn; its AvailableFunctions is the full toolset (null or empty means the turn has no known toolset), its ids identify the turn in the log.</param>
    /// <param name="mode">Whether an outside call only gets flagged or is also rejected.</param>
    /// <param name="logger">Receives one warning per outside call.</param>
    internal static List<LLMFunctionCall> Apply(
        List<LLMFunctionCall> functionCalls,
        LLMContext context,
        ToolsetMembershipMode mode,
        ILogger logger)
    {
        var turnToolset = context.AvailableFunctions;
        if (turnToolset is not { Count: > 0 })
        {
            return functionCalls;
        }

        var offeredNames = new HashSet<string>(turnToolset.Select(function => function.Name), StringComparer.OrdinalIgnoreCase);
        var executable = new List<LLMFunctionCall>(functionCalls.Count);
        foreach (var call in functionCalls)
        {
            if (IsOffered(call.FunctionName, offeredNames))
            {
                executable.Add(call);
                continue;
            }

            call.IsOutsideToolset = true;
            logger.LogWarning(
                "Model called {FunctionName}, which is not in the turn toolset of {ToolsetSize} functions "
                + "(mode {Mode}, turn {TurnId}, conversation {ConversationId}, model {ModelId})",
                call.FunctionName, turnToolset.Count, mode, context.TurnId, context.ConversationId, context.ModelId);

            if (mode != ToolsetMembershipMode.Enforce)
            {
                executable.Add(call);
                continue;
            }

            call.Success = false;
            call.IsRejectedOutsideToolset = true;
            call.Result = LLMLoopConstants.ToolsetMembershipRejectedResult;
        }

        return executable;
    }

    private static bool IsOffered(string? functionName, HashSet<string> offeredNames)
    {
        if (string.IsNullOrWhiteSpace(functionName))
        {
            return false;
        }

        var skillName = SkillNameAliases.Resolve(functionName);
        return offeredNames.Contains(functionName)
            || offeredNames.Contains(skillName)
            || string.Equals(skillName, AutonomyDefaults.ConfirmPendingActionSkillName, StringComparison.OrdinalIgnoreCase);
    }
}
