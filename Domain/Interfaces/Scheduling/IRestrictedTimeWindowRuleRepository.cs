// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Persistence access for <see cref="Klacks.Api.Domain.Models.Scheduling.RestrictedTimeWindowRule"/> rows:
/// lookup by the region-setup entity-import natural keys for the re-apply reconciliation, and the active
/// rule set used by RestrictedTimeWindowEvaluator and the wizard restricted-window context builder.
/// </summary>

using Klacks.Api.Domain.Models.Scheduling;

namespace Klacks.Api.Domain.Interfaces.Scheduling;

public interface IRestrictedTimeWindowRuleRepository
{
    Task<List<RestrictedTimeWindowRule>> GetBySourceKeysAsync(IReadOnlyCollection<string> sourceKeys);

    Task<List<RestrictedTimeWindowRule>> GetAllActiveAsync();

    Task<RestrictedTimeWindowRule?> GetAsync(Guid id);

    Task<RestrictedTimeWindowRule?> DeleteAsync(Guid id);

    void Add(RestrictedTimeWindowRule rule);

    void Update(RestrictedTimeWindowRule rule);
}
