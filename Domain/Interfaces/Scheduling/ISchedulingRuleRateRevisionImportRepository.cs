// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Persistence access for the region-setup entity-import path (K20) over
/// <see cref="Klacks.Api.Domain.Models.Scheduling.SchedulingRuleRateRevision"/> rows: lookup by natural
/// import keys for the re-apply reconciliation, plus the insert/update primitives the import decisions
/// execute. Runtime rate resolution reads revisions directly through the DbContext, never through this one.
/// </summary>

using Klacks.Api.Domain.Models.Scheduling;

namespace Klacks.Api.Domain.Interfaces.Scheduling;

public interface ISchedulingRuleRateRevisionImportRepository
{
    Task<List<SchedulingRuleRateRevision>> GetBySourceKeysAsync(IReadOnlyCollection<string> sourceKeys);

    void Add(SchedulingRuleRateRevision revision);

    void Update(SchedulingRuleRateRevision revision);
}
