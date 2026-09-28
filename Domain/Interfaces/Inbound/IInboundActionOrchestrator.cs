// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Inbound;

namespace Klacks.Api.Domain.Interfaces.Inbound;

public interface IInboundActionOrchestrator
{
    Task<InboundActionOutcome?> ExecuteAsync(
        Guid clientId, InboundSource source, InboundAnalysis analysis, CancellationToken cancellationToken = default);
}
