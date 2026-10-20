// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Services.Assistant;

public interface ISkillCacheService
{
    Task<Agent?> GetDefaultAgentAsync(CancellationToken ct = default);
    Task<IReadOnlyList<AgentSkill>> GetEnabledSkillsAsync(Guid agentId, CancellationToken ct = default);

    /// <summary>
    /// Every enabled, non-deleted skill of every agent — the exact result of
    /// IAgentSkillRepository.GetAllEnabledAsync, cached. Deliberately not expressed through
    /// GetEnabledSkillsAsync: that one is scoped to a single agent and does not filter soft-deleted
    /// rows, so the two are not interchangeable.
    /// </summary>
    Task<IReadOnlyList<AgentSkill>> GetAllEnabledSkillsAsync(CancellationToken ct = default);

    void InvalidateCache();
    Task WarmupAsync(CancellationToken ct = default);
}
