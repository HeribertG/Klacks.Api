// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Iteration / step limits for the LLM chat multi-turn loop and the autonomy plan executor.
/// Kept here so chat-loop tuning and plan-budget tuning live in one place instead of being
/// scattered as magic numbers across LLMService and PlanStepExecutor.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class LLMLoopConstants
{
    // Guided workflows chain several tool round-trips before answering (e.g. assigning a
    // contract does lookup_location + list_contracts + list_groups, then needs one more
    // iteration to present the choices). A cap of 3 exhausted the budget on the lookups and
    // the loop ended before the assistant could present results — the user saw a "hang".
    public const int MaxChatToolIterations = 6;

    public const int MaxPlanSteps = 15;

    // A plan step runs a deterministic skill call (no LLM at runtime). A single retry recovers from
    // a transient backend hiccup (rate limit / gateway blip) without risking a double mutation on a
    // step that genuinely failed. Classification + backoff reuse LLMRetryConstants systematics.
    public const int MaxPlanStepTransientRetries = 1;

    // Tool result returned instead of executing a side-effecting skill a second time within one
    // turn. The toolset is kept identical across loop iterations so provider prompt caches stay
    // valid; the once-per-turn rule for write skills is enforced at execution time via this
    // rejection, not by shrinking the tool array.
    public const string RepeatedWriteCallRejectedResult =
        "Rejected: this action already ran in this turn and must not run twice. " +
        "Use its earlier result from the previous function results instead of calling it again.";
}
