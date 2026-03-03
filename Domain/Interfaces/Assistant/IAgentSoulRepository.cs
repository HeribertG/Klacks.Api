// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IAgentSoulRepository
{
    Task<List<AgentSoulSection>> GetActiveSectionsAsync(Guid agentId, CancellationToken cancellationToken = default);
    Task<AgentSoulSection?> GetSectionAsync(Guid agentId, string sectionType, CancellationToken cancellationToken = default);
    Task<AgentSoulSection> UpsertSectionAsync(Guid agentId, string sectionType, string content, int sortOrder, string? source = null, string? changedBy = null, CancellationToken cancellationToken = default);
    Task DeactivateSectionAsync(Guid agentId, string sectionType, CancellationToken cancellationToken = default);
    Task<List<AgentSoulHistory>> GetHistoryAsync(Guid agentId, int limit = 50, CancellationToken cancellationToken = default);
}
