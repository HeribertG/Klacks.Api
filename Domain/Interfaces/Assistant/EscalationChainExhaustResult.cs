// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Assistant.Escalation;

namespace Klacks.Api.Domain.Interfaces.Assistant;

/// <param name="Exhausted">Whether THIS call won the chain's Running to Exhausted compare-and-swap.</param>
/// <param name="CancelledNotifiedStages">The stages this same call moved from Notified to Cancelled, read
/// back inside the exhaust transaction so the caller can close their open inbox rows. Empty whenever the
/// compare-and-swap was lost, and never contains a stage another caller resolved in the meantime.</param>
public readonly record struct EscalationChainExhaustResult(
    bool Exhausted, IReadOnlyList<EscalationStage> CancelledNotifiedStages)
{
    public static EscalationChainExhaustResult Lost { get; } = new(false, []);
}
