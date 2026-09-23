// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Outcome of the clarification policy: Ask (send a question), Suggest (propose to the planner without
/// sending, used at autonomy level Propose or while the kill switch is active) or Skip (with a reason).
/// </summary>
/// <param name="Kind">Ask, Suggest or Skip</param>
/// <param name="SkipReason">Set only when Kind is Skip, otherwise null</param>

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Inbound;

public sealed record ClarificationDecision(ClarificationDecisionKind Kind, ClarificationSkipReason? SkipReason)
{
    public static ClarificationDecision Ask { get; } = new(ClarificationDecisionKind.Ask, null);

    public static ClarificationDecision Suggest { get; } = new(ClarificationDecisionKind.Suggest, null);

    public static ClarificationDecision Skip(ClarificationSkipReason reason) => new(ClarificationDecisionKind.Skip, reason);
}
