// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.Interfaces.Assistant;

public interface IRetrievalQueryBuilder
{
    Task<string> BuildAsync(string userMessage, string? conversationId, string userId, CancellationToken cancellationToken = default);
}
