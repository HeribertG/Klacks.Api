// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.KnowledgeIndex.Domain;

namespace Klacks.Api.KnowledgeIndex.Application.Interfaces;

public interface IKnowledgeRetrievalService
{
    Task<RetrievalResult> RetrieveAsync(
        string userQuery,
        IReadOnlyCollection<string> userPermissions,
        bool isAdmin,
        int topK,
        CancellationToken cancellationToken);
}
