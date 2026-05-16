// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Repository for persistent AgentTriggerPreferenceRow rows. Single-entry per (UserId, TriggerKind).
/// </summary>

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IAgentTriggerPreferenceRepository
{
    Task<AgentTriggerPreferenceRow?> GetAsync(string userId, string triggerKind, CancellationToken cancellationToken = default);

    Task<AgentTriggerPreferenceRow> UpsertAsync(AgentTriggerPreferenceRow row, CancellationToken cancellationToken = default);
}
