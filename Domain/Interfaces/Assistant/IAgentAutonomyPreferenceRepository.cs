// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IAgentAutonomyPreferenceRepository
{
    Task<AgentAutonomyPreferenceRow?> GetAsync(string userId, CancellationToken cancellationToken = default);

    Task<AgentAutonomyPreferenceRow> UpsertAsync(AgentAutonomyPreferenceRow row, CancellationToken cancellationToken = default);
}
