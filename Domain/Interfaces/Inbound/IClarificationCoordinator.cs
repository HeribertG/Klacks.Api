// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Inbound;

namespace Klacks.Api.Domain.Interfaces.Inbound;

public interface IClarificationCoordinator
{
    Task<ClarificationPreAnalysis> BeforeAnalysisAsync(ClarificationRequest request, CancellationToken cancellationToken = default);

    Task<ClarificationPostAnalysis> AfterAnalysisAsync(
        ClarificationRequest request, InboundAnalysis analysis, CancellationToken cancellationToken = default);
}
