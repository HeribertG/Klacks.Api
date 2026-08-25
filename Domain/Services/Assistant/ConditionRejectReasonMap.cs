// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Translates a planner's rejection of a prepared scenario into the ledger's reason vocabulary. The two
/// enums answer different questions and are not two spellings of one list: RejectReason says what was
/// wrong with THIS proposal (coverage dropped, hours unbalanced, too much churn), while
/// AgentConditionRejectReason says what Klacksy should learn about the FINDING (never raise this,
/// wrong right now, someone already handled it). Every substantive objection therefore lands on
/// WrongThisTime - it faults the proposal, not the finding, and the condition may legitimately be
/// raised again tomorrow. GenerallyUnwanted is deliberately unreachable from here: refusing one
/// scenario is not a statement about the kind, and only the dismissal menu on the notification itself
/// (Etappe 3d) can say that. Both an absent reason and Unspecified map to NoReason, since neither
/// carries a learnable signal.
/// </summary>

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Services.Assistant;

public static class ConditionRejectReasonMap
{
    public static AgentConditionRejectReason FromScenarioRejection(RejectReason? scenarioReason) =>
        scenarioReason switch
        {
            null => AgentConditionRejectReason.NoReason,
            RejectReason.Unspecified => AgentConditionRejectReason.NoReason,
            _ => AgentConditionRejectReason.WrongThisTime
        };
}
