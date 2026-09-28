// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Inbound;

namespace Klacks.Api.Domain.Interfaces.Inbound;

public interface IInboundAnalysisNotifier
{
    Task NotifyAsync(
        InboundSource source,
        InboundAnalysis analysis,
        InboundActionOutcome? actionOutcome = null,
        string? periodLoadSummary = null,
        string? clarificationContext = null,
        CancellationToken cancellationToken = default);

    Task NotifyMessageAsync(string message, CancellationToken cancellationToken = default);
}
