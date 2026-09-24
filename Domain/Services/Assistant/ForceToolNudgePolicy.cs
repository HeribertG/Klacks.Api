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
/// The nudge leaves one exchange in the running history (the user's message and the nudged answer);
/// WithoutNudgeExchange removes it again for a follow-up call that restates the user's message itself.
/// </summary>
/// <param name="isMutationIntent">The user message expressed a state-changing intent.</param>
/// <param name="forceConfirmation">A pending confirmation was being resolved this turn.</param>
/// <param name="containsMarkup">The response contained tool-call markup that never executed.</param>
/// <param name="claimsCompletion">The response claims a state change was already carried out.</param>
/// <param name="toolCallCount">Number of tool calls executed this turn.</param>
/// <param name="recipePausedOnAsk">A recipe deliberately paused on an ask step.</param>
/// <param name="isClarifyingResponse">The response is a clarifying question or a [REPLIES:] affordance.</param>

using Klacks.Api.Domain.Constants;
using LLMMessage = Klacks.Api.Domain.Services.Assistant.Providers.LLMMessage;

namespace Klacks.Api.Domain.Services.Assistant;

public static class ForceToolNudgePolicy
{
    private const int NudgeExchangeLength = 2;

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

    /// <summary>
    /// A copy of the running history without the exchange the nudge appended, so a follow-up call that
    /// sends the user's message again does not carry it twice. The history is returned unchanged (as a
    /// copy) when its tail is not that exchange, e.g. after the budget fit already dropped part of it.
    /// The running history itself is never modified.
    /// </summary>
    /// <param name="runningHistory">The loop's running history after the nudged iteration.</param>
    /// <param name="userMessage">The user's message the nudge exchange starts with.</param>
    internal static List<LLMMessage> WithoutNudgeExchange(IReadOnlyList<LLMMessage> runningHistory, string userMessage)
    {
        var copy = new List<LLMMessage>(runningHistory);
        if (copy.Count < NudgeExchangeLength)
        {
            return copy;
        }

        var exchangeStart = copy[^NudgeExchangeLength];
        var endsWithTheExchange =
            string.Equals(exchangeStart.Role, LLMMessageRoles.User, StringComparison.Ordinal)
            && string.Equals(exchangeStart.Content, userMessage, StringComparison.Ordinal)
            && string.Equals(copy[^1].Role, LLMMessageRoles.Assistant, StringComparison.Ordinal);
        if (endsWithTheExchange)
        {
            copy.RemoveRange(copy.Count - NudgeExchangeLength, NudgeExchangeLength);
        }

        return copy;
    }
}
