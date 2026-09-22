// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Inbound;

namespace Klacks.Api.Domain.Interfaces.Inbound;

public interface IInboundIntentAnalysisService
{
    Task<InboundAnalysis> AnalyzeAsync(
        Guid clientId, EntityTypeEnum clientType, InboundSource source, CancellationToken cancellationToken = default);
}
