// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Mutable per-tick counters of the action dispatcher, folded into the immutable
/// <see cref="AgentConditionActionTickResult"/> at the end of the run. One property per outcome so
/// that no way of passing a row over can go uncounted.
/// </summary>

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Conditions;

internal sealed class ConditionActionTally
{
    public int Considered { get; set; }

    public int Executed { get; set; }

    public int Failed { get; set; }

    public int Escalated { get; set; }

    public int SkippedCascade { get; set; }

    public int SkippedQuiet { get; set; }

    public int SkippedUnbindable { get; set; }

    public int SkippedNoApprover { get; set; }

    public int SkippedClaimLost { get; set; }

    public int LeftForBudget { get; set; }

    public int ApprovalsRequested { get; set; }

    public int AwaitingApproval { get; set; }

    public int ApprovalsUnavailable { get; set; }

    public int ApprovalsWithdrawn { get; set; }

    public AgentConditionActionTickResult ToResult() => new(
        Considered, Executed, Failed, Escalated, SkippedCascade, SkippedQuiet,
        SkippedUnbindable, SkippedNoApprover, SkippedClaimLost, LeftForBudget,
        ApprovalsRequested, AwaitingApproval, ApprovalsUnavailable, ApprovalsWithdrawn);
}
