// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Persistence access for <see cref="Klacks.Api.Domain.Models.Scheduling.CounterRule"/> rows: lookup by
/// the region-setup entity-import natural keys for the re-apply reconciliation, and the active rule set
/// used by CounterRuleEvaluator.
/// </summary>

using Klacks.Api.Domain.Models.Scheduling;

namespace Klacks.Api.Domain.Interfaces.Scheduling;

public interface ICounterRuleRepository
{
    Task<List<CounterRule>> GetBySourceKeysAsync(IReadOnlyCollection<string> sourceKeys);

    Task<List<CounterRule>> GetAllActiveAsync();

    void Add(CounterRule rule);

    void Update(CounterRule rule);
}
