// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.Interfaces.Assistant;

public interface IRetrievalQueryBuilder
{
    Task<string> BuildAsync(string userMessage, string? conversationId, string userId, CancellationToken cancellationToken = default);
}
