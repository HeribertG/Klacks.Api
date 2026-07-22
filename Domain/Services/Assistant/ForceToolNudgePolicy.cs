// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Decides whether the NON-STREAMING chat path should retry the turn once with a tool-forcing nudge.
/// Intentionally kept as its own class (separate from NoActionNoticePolicy) to lock the deliberate
/// streaming/non-streaming asymmetry: non-streaming buffers the whole answer, so a false success can
/// still be suppressed by forcing a real tool call instead of announcing the failure. The nudge is
/// warranted when the turn signalled a state change — a mutation intent, a pending confirmation,
/// self-emitted tool-call markup that never executed, or an assistant completion claim — yet produced
/// zero tool calls, and the response is neither a paused recipe ask nor a clarifying question. Loop
/// guards (retry-already-used, iteration budget) stay at the call site and are not part of this predicate.
/// </summary>
/// <param name="isMutationIntent">The user message expressed a state-changing intent.</param>
/// <param name="forceConfirmation">A pending confirmation was being resolved this turn.</param>
/// <param name="containsMarkup">The response contained tool-call markup that never executed.</param>
/// <param name="claimsCompletion">The response claims a state change was already carried out.</param>
/// <param name="toolCallCount">Number of tool calls executed this turn.</param>
/// <param name="recipePausedOnAsk">A recipe deliberately paused on an ask step.</param>
/// <param name="isClarifyingResponse">The response is a clarifying question or a [REPLIES:] affordance.</param>

namespace Klacks.Api.Domain.Services.Assistant;

public static class ForceToolNudgePolicy
{
    public static bool ShouldForceToolNudge(
        bool isMutationIntent,
        bool forceConfirmation,
        bool containsMarkup,
        bool claimsCompletion,
        int toolCallCount,
        bool recipePausedOnAsk,
        bool isClarifyingResponse)
    {
        return (isMutationIntent || forceConfirmation || containsMarkup || claimsCompletion)
            && toolCallCount == 0
            && !recipePausedOnAsk
            && !isClarifyingResponse;
    }
}
