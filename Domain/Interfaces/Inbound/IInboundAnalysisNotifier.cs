// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Inbound;

namespace Klacks.Api.Domain.Interfaces.Inbound;

public interface IInboundAnalysisNotifier
{
    Task NotifyAsync(
        InboundSource source,
        InboundAnalysis analysis,
        InboundActionOutcome? actionOutcome = null,
        string? periodLoadSummary = null,
        CancellationToken cancellationToken = default);
}
