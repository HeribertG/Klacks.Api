// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Infrastructure.KnowledgeIndex.Domain;

namespace Klacks.Api.Infrastructure.KnowledgeIndex.Application.Interfaces;

public interface IKnowledgeIndexRepository
{
    Task<IReadOnlyDictionary<(KnowledgeEntryKind Kind, string SourceId), byte[]>> GetAllHashesAsync(CancellationToken ct);
    Task UpsertAsync(IReadOnlyList<KnowledgeEntry> entries, CancellationToken ct);
    Task DeleteAsync(IReadOnlyList<(KnowledgeEntryKind Kind, string SourceId)> keys, CancellationToken ct);

    Task<IReadOnlyList<KnowledgeEntry>> FindNearestAsync(
        float[] queryEmbedding,
        IReadOnlyCollection<string> userPermissions,
        bool adminBypass,
        int topN,
        CancellationToken ct);
}
