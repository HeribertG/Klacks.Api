// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Enums;

/// <summary>
/// Why an acknowledgement attempt ended the way it did. It replaces the former bool, which collapsed
/// "there was nothing of yours to acknowledge" and "you were too late, the chain is already resolved"
/// into the same false - and, worse, reported the second case as true whenever the stage-level
/// compare-and-swap was won while the chain-level one was lost, so an approval that never got stamped
/// looked like a success to every caller.
/// </summary>
public enum EscalationAcknowledgeOutcome
{
    Acknowledged = 0,
    NoNotifiedStage = 1,
    ChainAlreadyResolved = 2
}
