// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// "Mach du" (Etappe 4e): a planner grants Klacksy a single-condition permission to go further than the
/// trigger kind's own governance allows. Keyed by the proactive-message dispatch row id - the same id
/// SetProactiveReactionCommand already uses - rather than the condition id itself, so the finding row's
/// action buttons stay uniform and the handler, not the client, resolves which condition-ledger row the
/// message reported (mirroring how SetProactiveReactionCommandHandler resolves a dismissal's reject
/// reason onto its ConditionId).
/// </summary>

using Klacks.Api.Domain.Enums;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Assistant;

public class DelegateConditionCommand : IRequest<DelegateConditionOutcome>
{
    public Guid MessageId { get; set; }

    public Guid DelegatingUserId { get; set; }

    /// <summary>Prepare or Execute; Hint is rejected upstream in the controller as nothing to delegate.</summary>
    public ProactiveMaxAction MaxAction { get; set; }
}
