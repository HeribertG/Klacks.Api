// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Inbound;

namespace Klacks.Api.Domain.Interfaces.Inbound;

public interface IInboundActionOrchestrator
{
    Task<InboundActionOutcome?> ExecuteAsync(
        Guid clientId, InboundSource source, InboundAnalysis analysis, CancellationToken cancellationToken = default);
}
