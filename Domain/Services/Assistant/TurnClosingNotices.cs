// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// The two texts a chat turn may have to append after its tool loop ended, both of them reaching the user
/// without passing through the model.
///
/// No-action notice (streaming only): the lie is already on screen, because content streams
/// token-by-token before the loop ends, so it cannot be retracted - an honest correction is appended
/// instead. A mutation request that produced zero tool calls means nothing happened, regardless of any
/// prose claim. The same applies when intent detection missed the phrasing but the model emitted a text
/// tool-call itself (e.g. "&lt;function_calls&gt;..." for a non-existent skill): that markup never
/// executes, so a zero-real-tool-call turn containing it is the same no-action lie. A clarifying question
/// (or a [REPLIES:] affordance) is not a false success claim and is skipped, otherwise the well-behaved
/// default path (Gemini/Anthropic ignore tool_choice) would regress; a recipe deliberately paused on an
/// ask is not a no-action lie either. The non-streaming path has no equivalent: nothing is sent before the
/// loop ends there, so it suppresses the claim with a forced retry (ForceToolNudgePolicy) instead, and a
/// claim made by its empty-answer recovery call is replaced by EmptyAnswerRecovery with the no-action notice.
///
/// Step-failed notice (both paths): a forced recipe step (tool_choice=required) can fail on every
/// iteration until the iteration budget is exhausted - e.g. a name-resolution skill rejecting the model's
/// guess each time. Function-call turns typically carry no prose, so the response stays blank and the user
/// would see literally nothing. The last failure's own message is already actionable (it often lists the
/// real options), so it is surfaced. Because it bypasses the model entirely, no prompt rule can strip
/// internal names from it, which is why it is redacted here while the caller logs the raw message.
/// </summary>
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Services.Assistant.Providers;

namespace Klacks.Api.Domain.Services.Assistant;

internal static class TurnClosingNotices
{
    /// <summary>
    /// The no-action notice for a streaming turn, or null when the turn made no false success claim.
    /// </summary>
    /// <param name="isMutationIntent">True when the user's message was detected as a mutation request.</param>
    /// <param name="forceConfirmation">True while the pending-confirmation gate narrowed the turn.</param>
    /// <param name="responseContent">Everything the turn has streamed so far.</param>
    /// <param name="functionCallCount">Tool calls the turn actually made.</param>
    /// <param name="recipePausedOnAsk">True when the turn deliberately stopped on a recipe ask step.</param>
    /// <param name="isClarifyingResponse">True when the answer asks the user something instead of claiming an action.</param>
    internal static string? NoAction(
        bool isMutationIntent,
        bool forceConfirmation,
        string responseContent,
        int functionCallCount,
        bool recipePausedOnAsk,
        bool isClarifyingResponse) =>
        NoActionNoticePolicy.ShouldAppendNotice(
            isMutationIntent,
            forceConfirmation,
            ToolCallMarkupSanitizer.ContainsMarkup(responseContent),
            CompletionClaimDetector.ClaimsCompletion(responseContent),
            functionCallCount,
            recipePausedOnAsk,
            isClarifyingResponse)
            ? MutationGuardConstants.NoActionStreamNotice
            : null;

    /// <summary>
    /// The call whose failure message has to be surfaced because the turn produced no prose at all and
    /// every call it made failed, or null when there is nothing to surface. A rejected repeat carries only
    /// the generic rejection text, so the genuine failure of an earlier iteration is preferred.
    /// </summary>
    /// <param name="allFunctionCalls">Every call of the turn, in call order.</param>
    /// <param name="responseContent">The turn's answer; a non-blank answer suppresses the notice.</param>
    internal static LLMFunctionCall? LastUnrecoveredFailure(
        IReadOnlyList<LLMFunctionCall> allFunctionCalls, string responseContent)
    {
        if (!string.IsNullOrWhiteSpace(responseContent)
            || allFunctionCalls.Count == 0
            || allFunctionCalls.Any(call => call.Success))
        {
            return null;
        }

        return allFunctionCalls.LastOrDefault(call => !call.IsRejectedRepeat) ?? allFunctionCalls[^1];
    }

    /// <summary>The user-visible, identifier-redacted text for a failed forced step.</summary>
    /// <param name="failedCall">The call picked by <see cref="LastUnrecoveredFailure"/>.</param>
    internal static string StepFailed(LLMFunctionCall failedCall) =>
        MutationGuardConstants.RecipeStepFailedNoticePrefix
        + InternalIdentifierRedactor.Redact(failedCall.Result);
}
