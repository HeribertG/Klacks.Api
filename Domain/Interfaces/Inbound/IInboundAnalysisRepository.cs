// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Inbound;

namespace Klacks.Api.Domain.Interfaces.Inbound;

public interface IInboundAnalysisRepository
{
    Task AddAsync(InboundAnalysis analysis, CancellationToken cancellationToken = default);

    Task<InboundAnalysis?> GetBySourceAsync(
        InboundSourceKind sourceKind, Guid sourceId, CancellationToken cancellationToken = default);
}
