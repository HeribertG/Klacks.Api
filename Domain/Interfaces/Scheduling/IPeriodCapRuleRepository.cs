// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Persistence access for <see cref="Klacks.Api.Domain.Models.Scheduling.PeriodCapRule"/> rows: lookup
/// by the region-setup entity-import natural keys for the re-apply reconciliation, and the active rule
/// set used by PeriodCapEvaluator.
/// </summary>

using Klacks.Api.Domain.Models.Scheduling;

namespace Klacks.Api.Domain.Interfaces.Scheduling;

public interface IPeriodCapRuleRepository
{
    Task<List<PeriodCapRule>> GetBySourceKeysAsync(IReadOnlyCollection<string> sourceKeys);

    Task<List<PeriodCapRule>> GetAllActiveAsync();

    Task<PeriodCapRule?> GetAsync(Guid id);

    Task<PeriodCapRule?> DeleteAsync(Guid id);

    void Add(PeriodCapRule rule);

    void Update(PeriodCapRule rule);
}
