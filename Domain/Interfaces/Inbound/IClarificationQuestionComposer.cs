// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Inbound;

namespace Klacks.Api.Domain.Interfaces.Inbound;

public interface IClarificationQuestionComposer
{
    Task<ComposedClarificationQuestion?> ComposeAsync(
        ClarificationRequest request, InboundAnalysis analysis, CancellationToken cancellationToken = default);
}
