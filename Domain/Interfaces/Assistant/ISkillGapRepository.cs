// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Repository for persisting and querying SkillGapRecord entities.
/// </summary>

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface ISkillGapRepository
{
    Task<SkillGapRecord?> FindSimilarAsync(Guid agentId, float[] embedding, float threshold, CancellationToken cancellationToken = default);

    Task AddAsync(SkillGapRecord record, CancellationToken cancellationToken = default);

    Task UpdateAsync(SkillGapRecord record, CancellationToken cancellationToken = default);

    Task<List<SkillGapRecord>> GetPendingAsync(Guid agentId, int minOccurrences, CancellationToken cancellationToken = default);
}
