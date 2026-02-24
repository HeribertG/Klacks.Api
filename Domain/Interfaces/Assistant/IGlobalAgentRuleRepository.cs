// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IGlobalAgentRuleRepository
{
    Task<List<GlobalAgentRule>> GetActiveRulesAsync(CancellationToken cancellationToken = default);
    Task<GlobalAgentRule?> GetRuleAsync(string name, CancellationToken cancellationToken = default);
    Task<GlobalAgentRule> UpsertRuleAsync(string name, string content, int sortOrder, string? source = null, string? changedBy = null, CancellationToken cancellationToken = default);
    Task DeactivateRuleAsync(string name, CancellationToken cancellationToken = default);
    Task<List<GlobalAgentRuleHistory>> GetHistoryAsync(int limit = 50, CancellationToken cancellationToken = default);
}
