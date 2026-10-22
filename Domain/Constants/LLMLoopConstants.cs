// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Iteration / step limits for the LLM chat multi-turn loop and the autonomy plan executor.
/// Kept here so chat-loop tuning and plan-budget tuning live in one place instead of being
/// scattered as magic numbers across LLMService and PlanStepExecutor.
/// </summary>

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Constants;

public static class LLMLoopConstants
{
    // Guided workflows chain several tool round-trips before answering (e.g. assigning a
    // contract does lookup_location + list_contracts + list_groups, then needs one more
    // iteration to present the choices). A cap of 3 exhausted the budget on the lookups and
    // the loop ended before the assistant could present results — the user saw a "hang".
    public const int MaxChatToolIterations = 6;

    public const int MaxPlanSteps = 15;

    /// <summary>
    /// Per-result character cap for a tool result handed to a model when no budget profile sets one: the
    /// chat loop's fallback and the MCP text block of an untrusted result, so one huge payload cannot flood
    /// the reading model's context.
    /// </summary>
    public const int DefaultMaxToolResultChars = 8_000;

    // A plan step runs a deterministic skill call (no LLM at runtime). A single retry recovers from
    // a transient backend hiccup (rate limit / gateway blip) without risking a double mutation on a
    // step that genuinely failed. Classification + backoff reuse LLMRetryConstants systematics.
    public const int MaxPlanStepTransientRetries = 1;

    // Tool result returned instead of executing a side-effecting skill a second time within one
    // turn. The toolset is kept identical across loop iterations so provider prompt caches stay
    // valid; the once-per-turn rule for write skills is enforced at execution time via this
    // rejection, not by shrinking the tool array.
    public const string ReadOnlyRecipeWriteRejectedResult =
        "Rejected: this turn ran a read-only guided flow, so nothing may be stored, closed or confirmed in " +
        "it and this action did NOT run. Answer from the flow's result and offer the action as a separate " +
        "request the user can send; never say that it was carried out.";

    public const string CompletedRecipeWriteRejectedResult =
        "Rejected: the last step of this guided flow has already run and its result is above, so nothing " +
        "further may be stored, changed or confirmed in this turn and this action did NOT run. Reply to the " +
        "user from that result now; any follow-up change needs the user's answer first.";

    public const string NameAskBackWriteRejectedResult =
        "Rejected: an earlier call in this turn asked the user to confirm or choose a name, so nothing may be " +
        "stored, changed or confirmed before the user has answered and this action did NOT run. Ask the user now.";

    public const string RepeatedWriteCallRejectedResult =
        "Rejected: this action already ran in this turn and must not run twice. " +
        "Use its earlier result from the previous function results instead of calling it again.";

    public const string ToolsetMembershipRejectedResult =
        "Rejected: this function is not available in this conversation turn and it did NOT run. " +
        "Use only the functions you were given. If none of them fits the request, tell the user plainly " +
        "that you cannot do it here; never say that it was carried out.";

    /// <summary>
    /// What the chat loop does with a call to a skill outside the turn's toolset. Observe until the logged
    /// rate of such calls shows that no legitimate call is caught: the guard logs every outside call, and the
    /// function executor logs its outcome (Success, ResultKind), so the rate of outside calls that succeeded is
    /// countable. Switching to Enforce is this constant; the rest of the Enforce path is already in place: a
    /// rejected call is flagged (LLMFunctionCall.IsRejectedWithoutRunning), left out of the functions_called record
    /// (TurnTelemetry.SerializeFunctionsCalled), never counted as a skill failure by the failure consumers, never
    /// offered as the failure to surface (TurnClosingNotices.LastUnrecoveredFailure), and it counts as a failed
    /// write attempt for the nothing-stored notice (TurnClosingNotices.NothingExecuted).
    /// </summary>
    public const ToolsetMembershipMode DefaultToolsetMembershipMode = ToolsetMembershipMode.Observe;

    /// <summary>
    /// Former assistant stand-in for tool-call iterations. No longer written into the running history
    /// (models copied it verbatim as their final answer); kept so an echo of it is still recognised.
    /// </summary>
    public const string ExecutingFunctionCallsPlaceholder = "[Executing function calls]";

    /// <summary>
    /// Former assistant stand-in for a forced-retry iteration that produced no prose; retired like
    /// ExecutingFunctionCallsPlaceholder and kept only so an echo of it is still recognised.
    /// </summary>
    public const string NoActionTakenPlaceholder = "[no action taken]";

    /// <summary>
    /// Former assistant stand-in of the read-only research loop for a tool-call iteration without prose;
    /// retired like ExecutingFunctionCallsPlaceholder and kept only so an echo of it is still recognised.
    /// </summary>
    public const string GatheringDataPlaceholder = "[gathering data]";

    /// <summary>
    /// Every retired bracketed stand-in. None of them is written anymore, but an older stored answer may
    /// still carry one, so a model echoing it is still caught.
    /// </summary>
    public static readonly IReadOnlyList<string> RetiredPlaceholders =
    [
        ExecutingFunctionCallsPlaceholder,
        NoActionTakenPlaceholder,
        GatheringDataPlaceholder
    ];

    /// <summary>
    /// Assistant stand-in for a forced-retry iteration in which the model produced neither prose nor a
    /// tool call: a plain sentence, like the tool-call note, instead of a bracketed status marker.
    /// </summary>
    public const string NoActionHistoryNote = "(No tool was called in this step.)";

    /// <summary>
    /// Start of the assistant stand-in for a tool-call iteration without prose: a plain sentence rather
    /// than a bracketed status marker, so a model reading its own history has nothing marker-like to copy.
    /// </summary>
    public const string ToolCallHistoryNotePrefix = "(Called tools: ";

    public const string ToolCallHistoryNoteSuffix = ". Their results follow.)";

    public const string ToolCallHistoryNoteSeparator = ", ";
}
