// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Decides whether the STREAMING chat path must append the honest "no action was performed" notice
/// after a turn ended. Intentionally kept as its own class (separate from ForceToolNudgePolicy) to
/// lock the deliberate streaming/non-streaming asymmetry: streaming can only append a correction
/// because content already reached the client token-by-token, whereas non-streaming can still retry.
/// The notice fires when the turn signalled a state change — a mutation intent, a pending
/// confirmation, self-emitted tool-call markup that never executed, or an assistant completion claim —
/// yet produced zero tool calls, and the response is neither a paused recipe ask nor a clarifying question.
/// </summary>
/// <param name="isMutationIntent">The user message expressed a state-changing intent.</param>
/// <param name="forceConfirmation">A pending confirmation was being resolved this turn.</param>
/// <param name="emittedTextToolCall">The response contained tool-call markup that never executed.</param>
/// <param name="claimsCompletion">The response claims a state change was already carried out.</param>
/// <param name="toolCallCount">Number of tool calls executed this turn.</param>
/// <param name="recipePausedOnAsk">A recipe deliberately paused on an ask step.</param>
/// <param name="isClarifyingResponse">The response is a clarifying question or a [REPLIES:] affordance.</param>

namespace Klacks.Api.Domain.Services.Assistant;

public static class NoActionNoticePolicy
{
    public static bool ShouldAppendNotice(
        bool isMutationIntent,
        bool forceConfirmation,
        bool emittedTextToolCall,
        bool claimsCompletion,
        int toolCallCount,
        bool recipePausedOnAsk,
        bool isClarifyingResponse)
    {
        return (isMutationIntent || forceConfirmation || emittedTextToolCall || claimsCompletion)
            && toolCallCount == 0
            && !recipePausedOnAsk
            && !isClarifyingResponse;
    }
}
