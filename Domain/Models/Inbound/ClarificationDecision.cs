// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Inbound;

public sealed record ClarificationDecision(ClarificationDecisionKind Kind, ClarificationSkipReason? SkipReason)
{
    public static ClarificationDecision Ask { get; } = new(ClarificationDecisionKind.Ask, null);

    public static ClarificationDecision Suggest { get; } = new(ClarificationDecisionKind.Suggest, null);

    public static ClarificationDecision Skip(ClarificationSkipReason reason) => new(ClarificationDecisionKind.Skip, reason);
}
