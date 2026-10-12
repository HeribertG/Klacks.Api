// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

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
/// ask is not a no-action lie either, and neither is an answer that itself states that nothing was done
/// (ClaimNegationDetector.DeniesCompletion, e.g. "Nein – gespeichert ist nichts"); tool-call markup still counts.
/// The non-streaming path has no notice: nothing is sent before the
/// loop ends there, so it suppresses the claim with a forced retry (ForceToolNudgePolicy) instead, and a
/// claim made by its empty-answer recovery call is replaced by EmptyAnswerRecovery with the no-action notice.
///
/// Step-failed notice (both paths): a forced recipe step (tool_choice=required) can fail on every
/// iteration until the iteration budget is exhausted - e.g. a name-resolution skill rejecting the model's
/// guess each time. Function-call turns typically carry no prose, so the response stays blank and the user
/// would see literally nothing. The last failure's own message is already actionable (it often lists the
/// real options), so it is surfaced. Because it bypasses the model entirely, no prompt rule can strip
/// internal names from it, which is why it is redacted here while the caller logs the raw message.
///
/// Nothing-stored notice (both paths): a read-only recipe (only ask/search steps) cannot store anything, and
/// every write call after its final step is rejected (ReadOnlyRecipeWriteGuard). A completion claim in such a
/// turn is therefore false by construction - live 2026-09-26 the reply said a lag of 3 days had been stored
/// while only the read skill ran. The claim cannot be retracted once streamed, so the notice states the truth
/// in the turn's language. The detector is applied sentence by sentence: over the whole reply an auxiliary of one
/// sentence and a participle of another ("... noch kein Nachlauf gespeichert. ... gewählt haben") made an honest
/// answer look like a claim (live 2026-09-26). A sentence that negates the claim ("Sie haben noch keinen Wert
/// gespeichert") is not a claim either (ClaimNegationDetector, de/en/fr/it). Whether a call of the turn was
/// side-effecting is judged by the same predicate the write guard uses (ReadOnlyRecipeWriteGuard.IsSideEffectFree),
/// so an explain_* call the guard lets through does not suppress the notice as if it had stored something.
///
/// Every-write-failed notice (both paths, same wording and same slot as the nothing-stored notice): a turn that
/// attempted side-effecting calls and had none of them succeed has stored nothing, whatever the model's prose says.
/// Unlike the notices above it does not depend on the prose claiming anything - a model that follows a failed
/// call with "Erledigt, der Vertrag ist angelegt" cannot be caught reliably by a claim detector - and is held back
/// only by a blank answer (the step-failed notice's case), a turn whose only calls are held confirmations or
/// successful previews, a partial success and an answer that itself denies having done anything. A write refused
/// without running counts as a failed attempt. The notice names the redacted reason of the last write that really
/// failed. See NothingExecuted.
/// </summary>
using System.Text.RegularExpressions;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Providers;

namespace Klacks.Api.Domain.Services.Assistant;

internal static class TurnClosingNotices
{
    private static readonly Regex SentenceBoundary = new(@"(?<=[.!?])\s+|[。！？\n]+", RegexOptions.Compiled);

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
            isClarifyingResponse,
            ClaimNegationDetector.DeniesCompletion(responseContent))
            ? MutationGuardConstants.NoActionStreamNotice
            : null;

    /// <summary>
    /// The nothing-stored notice for a turn that completed a read-only recipe and nevertheless claims a
    /// completed action, or an empty string when no notice is owed. Empty rather than null so both chat paths
    /// can append it unconditionally.
    /// </summary>
    /// <param name="readOnlyRecipeCompleted">True when a read-only recipe executed its final step this turn.</param>
    /// <param name="responseContent">The turn's answer.</param>
    /// <param name="allFunctionCalls">Every call of the turn; a successful side-effecting call suppresses the notice.</param>
    /// <param name="language">The turn's language, used to localize the notice.</param>
    /// <param name="availableFunctions">The turn's toolset, source of each skill's curated effect.</param>
    internal static string NothingStored(
        bool readOnlyRecipeCompleted,
        string responseContent,
        IReadOnlyList<LLMFunctionCall> allFunctionCalls,
        string? language,
        IReadOnlyList<LLMFunction>? availableFunctions = null)
    {
        if (!readOnlyRecipeCompleted
            || !SentenceBoundary.Split(responseContent).Any(ClaimsUnnegatedCompletion)
            || allFunctionCalls.Any(call =>
                call.Success && !ReadOnlyRecipeWriteGuard.IsSideEffectFree(call, availableFunctions)))
        {
            return string.Empty;
        }

        return NothingStoredText(language);
    }

    /// <summary>
    /// The notice for a turn whose every side-effecting call failed while the answer still has words: whatever
    /// that answer says, nothing was stored or changed, and a success-sounding sentence in it ("Erledigt, ich habe
    /// den Vertrag angelegt") is false. Empty when no notice is owed.
    /// A call that was rejected without running (a repeat, a skill outside the toolset, a write after a completed
    /// recipe) is an attempted write that stored nothing, so it counts as a failed attempt: a turn whose only
    /// write attempt was refused has stored nothing, which is what the notice says. A call the user's stop skipped
    /// is not an attempt. A call that is merely flagged as outside the toolset (Observe mode) did run with its real
    /// outcome and counts like any other.
    /// A preview call of a preview/apply skill (PreviewApplySkillCalls) that succeeded writes nothing by contract:
    /// it is neither an attempt nor a success, so "preview ok, apply failed" and "preview ok, apply rejected as a
    /// repeat" both owe the notice, while a turn that only previewed owes nothing. A held confirmation is judged per
    /// call: it is no attempt and no failure, so a turn that only holds owes nothing, but a write that failed next
    /// to a held one still owes the notice. One successful side-effecting call makes the turn a partial success.
    /// A blank answer owes nothing here: that is the every-step-failed notice, which carries the actionable failure
    /// message. An answer that itself denies having done anything (ClaimNegationDetector.DeniesCompletion) is
    /// honest and stays as it is. A clarifying question is not exempt: "Erledigt, noch etwas?" is a false success,
    /// and the notice after a question is still true.
    /// The wording is the nothing-stored notice, which every shipped language already carries, followed by the
    /// redacted failure message of the last write that really ran and failed. The German step-failed prefix is
    /// left out because it would mix languages in a turn of another language; a refusal never contributes its
    /// text, which is addressed to the model.
    /// </summary>
    /// <param name="responseContent">The turn's answer.</param>
    /// <param name="allFunctionCalls">Every call of the turn.</param>
    /// <param name="language">The turn's language, used to localize the notice.</param>
    /// <param name="availableFunctions">The turn's toolset, source of each skill's curated effect.</param>
    internal static string NothingExecuted(
        string responseContent,
        IReadOnlyList<LLMFunctionCall> allFunctionCalls,
        string? language,
        IReadOnlyList<LLMFunction>? availableFunctions = null)
    {
        if (string.IsNullOrWhiteSpace(responseContent)
            || ClaimNegationDetector.DeniesCompletion(responseContent))
        {
            return string.Empty;
        }

        var attemptedWrites = allFunctionCalls
            .Where(call => !call.SkippedByStop && !call.RequiresConfirmation)
            .Where(call => !ReadOnlyRecipeWriteGuard.IsSideEffectFree(call, availableFunctions))
            .Where(call => !IsSucceededPreview(call))
            .ToList();
        if (attemptedWrites.Count == 0 || attemptedWrites.Any(call => call.Success))
        {
            return string.Empty;
        }

        var failedWrite = attemptedWrites.LastOrDefault(
            call => !call.IsRejectedWithoutRunning && !string.IsNullOrWhiteSpace(call.Result));
        return failedWrite == null
            ? NothingStoredText(language)
            : NothingStoredText(language) + RecipeEngineDefaults.NothingStoredFailureReasonSeparator + FailureReason(failedWrite);
    }

    private static bool IsSucceededPreview(LLMFunctionCall call) =>
        call.Success && PreviewApplySkillCalls.IsPreviewCall(call.FunctionName, call.Parameters);

    private static string FailureReason(LLMFunctionCall failedCall) =>
        InternalIdentifierRedactor.Redact(failedCall.Result);

    /// <summary>
    /// The one notice a turn may owe about not having stored anything: the read-only-recipe claim notice, else the
    /// every-write-failed notice. Never both, because both say the same sentence. Empty when neither is owed.
    /// </summary>
    /// <param name="readOnlyRecipeCompleted">True when a read-only recipe executed its final step this turn.</param>
    /// <param name="responseContent">The turn's answer.</param>
    /// <param name="allFunctionCalls">Every call of the turn.</param>
    /// <param name="language">The turn's language, used to localize the notice.</param>
    /// <param name="availableFunctions">The turn's toolset, source of each skill's curated effect.</param>
    internal static string NothingChanged(
        bool readOnlyRecipeCompleted,
        string responseContent,
        IReadOnlyList<LLMFunctionCall> allFunctionCalls,
        string? language,
        IReadOnlyList<LLMFunction>? availableFunctions = null)
    {
        var nothingStored = NothingStored(
            readOnlyRecipeCompleted, responseContent, allFunctionCalls, language, availableFunctions);
        return nothingStored.Length > 0
            ? nothingStored
            : NothingExecuted(responseContent, allFunctionCalls, language, availableFunctions);
    }

    private static string NothingStoredText(string? language)
    {
        var text = GracefulCorrectionTexts.TryGetText(
            GracefulCorrectionTexts.RecipeNothingStoredNotice, language, out var localized)
            ? localized
            : RecipeEngineDefaults.NothingStoredNotice;
        return RecipeEngineDefaults.NothingStoredNoticeSeparator + text;
    }

    private static bool ClaimsUnnegatedCompletion(string sentence) =>
        CompletionClaimDetector.ClaimsCompletion(sentence) && !ClaimNegationDetector.IsNegated(sentence);

    /// <summary>
    /// The call whose failure message has to be surfaced because the turn produced no prose at all and
    /// every call it made failed, or null when there is nothing to surface. A call rejected without running (a
    /// repeat, a skill outside the toolset, a write after a completed recipe) carries only a rejection text that is
    /// addressed to the model, so the genuine failure of an earlier iteration is preferred and a turn in which every
    /// call was rejected surfaces nothing at all.
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

        return allFunctionCalls.LastOrDefault(call => !call.IsRejectedWithoutRunning);
    }

    /// <summary>
    /// The notices a streaming turn appends after its loop, in the order they reach the user: the no-action
    /// notice first, then the nothing-stored notice, then the every-step-failed notice, which is judged against the answer INCLUDING the
    /// first notice. Empty when the turn owes the user neither.
    /// </summary>
    /// <param name="isMutationIntent">True when the user's message was detected as a mutation request.</param>
    /// <param name="forceConfirmation">True while the pending-confirmation gate narrowed the turn.</param>
    /// <param name="responseContent">Everything the turn has streamed so far.</param>
    /// <param name="allFunctionCalls">Every call of the turn, in call order.</param>
    /// <param name="recipePausedOnAsk">True when the turn deliberately stopped on a recipe ask step.</param>
    /// <param name="logger">Receives the raw message of a surfaced failure, which the notice itself redacts.</param>
    /// <param name="readOnlyRecipeCompleted">True when a read-only recipe executed its final step this turn.</param>
    /// <param name="language">The turn's language, used to localize the nothing-stored notice.</param>
    /// <param name="availableFunctions">The turn's toolset, source of each skill's curated effect.</param>
    internal static IReadOnlyList<string> Collect(
        bool isMutationIntent,
        bool forceConfirmation,
        string responseContent,
        IReadOnlyList<LLMFunctionCall> allFunctionCalls,
        bool recipePausedOnAsk,
        ILogger logger,
        bool readOnlyRecipeCompleted = false,
        string? language = null,
        IReadOnlyList<LLMFunction>? availableFunctions = null)
    {
        var notices = new List<string>();
        var content = responseContent;

        var noActionNotice = NoAction(
            isMutationIntent, forceConfirmation, content,
            allFunctionCalls.Count, recipePausedOnAsk, ClarifyingResponse.IsClarifying(content));
        if (noActionNotice != null)
        {
            notices.Add(noActionNotice);
            content += noActionNotice;
        }

        var nothingStoredNotice = NothingChanged(
            readOnlyRecipeCompleted, content, allFunctionCalls, language, availableFunctions);
        if (nothingStoredNotice.Length > 0)
        {
            notices.Add(nothingStoredNotice);
            content += nothingStoredNotice;
        }

        var failedCall = LastUnrecoveredFailure(allFunctionCalls, content);
        if (failedCall != null)
        {
            logger.LogWarning(
                "All function calls failed in stream turn; surfacing notice for {FunctionName}. Raw result: {RawResult}",
                failedCall.FunctionName, failedCall.Result);
            notices.Add(StepFailed(failedCall));
        }

        return notices;
    }

    /// <summary>The user-visible, identifier-redacted text for a failed forced step.</summary>
    /// <param name="failedCall">The call picked by <see cref="LastUnrecoveredFailure"/>.</param>
    internal static string StepFailed(LLMFunctionCall failedCall) =>
        MutationGuardConstants.RecipeStepFailedNoticePrefix + FailureReason(failedCall);
}
