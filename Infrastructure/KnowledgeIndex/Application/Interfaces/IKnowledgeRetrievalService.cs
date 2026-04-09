// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Infrastructure.KnowledgeIndex.Domain;

namespace Klacks.Api.Infrastructure.KnowledgeIndex.Application.Interfaces;

public interface IKnowledgeRetrievalService
{
    Task<RetrievalResult> RetrieveAsync(
        string userQuery,
        IReadOnlyCollection<string> userPermissions,
        bool isAdmin,
        int topK,
        CancellationToken cancellationToken);
}
