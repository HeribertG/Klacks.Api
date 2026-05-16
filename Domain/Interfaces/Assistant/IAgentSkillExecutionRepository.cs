// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Read-only repository for AgentSkillExecution rows. Used by triggers and audit views
/// that need to inspect what skills ran when (e.g. LockConflictDetector correlates
/// failed mutating skill calls with lock_level errors).
/// </summary>

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IAgentSkillExecutionRepository
{
    Task<List<AgentSkillExecution>> GetSinceAsync(DateTime sinceUtc, CancellationToken cancellationToken = default);

    Task<List<AgentSkillExecution>> GetFailedSinceAsync(DateTime sinceUtc, CancellationToken cancellationToken = default);
}
