// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Inbound;

namespace Klacks.Api.Domain.Interfaces.Inbound;

public interface IClarificationQuestionComposer
{
    Task<ComposedClarificationQuestion?> ComposeAsync(
        ClarificationRequest request, InboundAnalysis analysis, CancellationToken cancellationToken = default);
}
