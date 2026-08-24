// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Enums;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Assistant;

public class SetProactiveReactionCommand : IRequest<bool>
{
    public Guid Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public ProactiveReaction Reaction { get; set; }

    /// <summary>
    /// Why the user rejected the finding. Only a Dismissed reaction carries one; null means no reason
    /// was stated, which a dismissal still reaches the condition ledger with - recorded there as
    /// NoReason, so a rejected ledger row always names a reason and the Etappe 6 learner never has to
    /// read a null as "not rejected". Whether the reason came from the picker or from the null default
    /// is deliberately not distinguished: the user declining to say and an older client not asking mean
    /// the same thing for the finding.
    /// </summary>
    public AgentConditionRejectReason? RejectReason { get; set; }
}
