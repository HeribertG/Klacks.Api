// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Persistence access for <see cref="Klacks.Api.Domain.Models.Scheduling.CounterRule"/> rows: lookup by
/// the region-setup entity-import natural keys for the re-apply reconciliation, the active rule set used
/// by CounterRuleEvaluator, and single-row CRUD for the customer-facing counter-rule commands.
/// </summary>

using Klacks.Api.Domain.Models.Scheduling;

namespace Klacks.Api.Domain.Interfaces.Scheduling;

public interface ICounterRuleRepository
{
    Task<List<CounterRule>> GetBySourceKeysAsync(IReadOnlyCollection<string> sourceKeys);

    Task<List<CounterRule>> GetAllActiveAsync();

    Task<CounterRule?> GetAsync(Guid id);

    Task<CounterRule?> DeleteAsync(Guid id);

    void Add(CounterRule rule);

    void Update(CounterRule rule);
}
