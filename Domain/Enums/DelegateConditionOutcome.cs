// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Enums;

/// <summary>
/// Result of a "mach du" delegation attempt (Etappe 4e). NotFound deliberately covers three different
/// causes - an unknown message id, a message that reported no condition-ledger finding, and a condition
/// outside the delegating user's own group-visibility scope - so the response never distinguishes "does
/// not exist" from "exists but you may not see it", matching how AgentConditionRepository's own scoped
/// queries already hide out-of-scope rows instead of revealing them. Forbidden is the one outcome that
/// DOES confirm the row's existence: it fires only once the user is already known to be a planner who
/// may see this exact condition, and answers only whether the requested MaxAction exceeds what their own
/// role may grant - a distinct, narrower question, not a discoverability leak.
/// </summary>
public enum DelegateConditionOutcome
{
    Delegated = 0,
    NotFound = 1,
    Forbidden = 2
}
