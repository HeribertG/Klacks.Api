// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Enums;

/// <summary>
/// Structured reason a human rejected a condition-ledger row's finding or its prepared remediation.
/// Distinct from Domain.Enums.RejectReason (AnalyseScenario rejects a plan candidate on plan-quality
/// dimensions); this enum reasons about whether Klacksy should have raised the finding at all, feeding
/// the future autonomy-hypothesis learner (Etappe 6).
/// </summary>
public enum AgentConditionRejectReason
{
    GenerallyUnwanted = 0,
    WrongThisTime = 1,
    AlreadyHandled = 2,
    NoReason = 3
}
