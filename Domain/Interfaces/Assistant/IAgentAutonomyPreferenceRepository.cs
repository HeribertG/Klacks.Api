// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IAgentAutonomyPreferenceRepository
{
    Task<AgentAutonomyPreferenceRow?> GetAsync(string userId, CancellationToken cancellationToken = default);

    Task<AgentAutonomyPreferenceRow> UpsertAsync(AgentAutonomyPreferenceRow row, CancellationToken cancellationToken = default);
}
