// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Inbound;

namespace Klacks.Api.Domain.Interfaces.Inbound;

public interface IInboundReplySender
{
    InboundSourceKind SourceKind { get; }

    Task<InboundReplyTarget?> ResolveTargetAsync(ClarificationRequest request, CancellationToken cancellationToken = default);

    Task<InboundReplyResult> SendAsync(
        ClarificationRequest request, InboundReplyTarget target, string text, CancellationToken cancellationToken = default);
}
