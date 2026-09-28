// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Inbound;

namespace Klacks.Api.Domain.Interfaces.Inbound;

public interface IInboundIntentAnalysisService
{
    Task<InboundAnalysis> AnalyzeAsync(
        Guid clientId, EntityTypeEnum clientType, InboundSource source, CancellationToken cancellationToken = default);

    Task<InboundAnalysis> AnalyzeAnswerAsync(
        Guid clientId,
        EntityTypeEnum clientType,
        InboundSource answerSource,
        ClarificationHistory history,
        CancellationToken cancellationToken = default);
}
