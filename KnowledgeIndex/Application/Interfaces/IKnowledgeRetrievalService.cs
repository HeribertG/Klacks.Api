// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.KnowledgeIndex.Domain;

namespace Klacks.Api.KnowledgeIndex.Application.Interfaces;

public interface IKnowledgeRetrievalService
{
    /// <summary>
    /// Ranks the indexed skills and recipes against the query and returns the best topK of them.
    /// </summary>
    /// <param name="userQuery">Text the candidates are scored against</param>
    /// <param name="userPermissions">Permissions the KNN pass filters the candidate set by</param>
    /// <param name="isAdmin">Bypasses the permission filter when true</param>
    /// <param name="topK">How many candidates are returned, applied after scoring and filtering</param>
    /// <param name="currentRoute">UI route whose skills receive a rank boost, or null</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <param name="kindFilter">Restricts the result to one entry kind. Applied BEFORE topK, so a
    /// caller that wants the best N recipes gets N recipes rather than the best N of everything
    /// filtered down afterwards. Null returns every kind.</param>
    Task<RetrievalResult> RetrieveAsync(
        string userQuery,
        IReadOnlyCollection<string> userPermissions,
        bool isAdmin,
        int topK,
        string? currentRoute,
        CancellationToken cancellationToken,
        KnowledgeEntryKind? kindFilter = null);
}
