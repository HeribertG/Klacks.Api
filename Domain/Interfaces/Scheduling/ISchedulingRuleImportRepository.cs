// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Persistence access for the region-setup entity-import path (K20) over
/// <see cref="Klacks.Api.Domain.Models.Scheduling.SchedulingRule"/> rows: lookup by natural import keys
/// for the re-apply reconciliation, plus the insert/update primitives the import decisions execute.
/// Customer CRUD goes through the regular SchedulingRule repository, never through this one.
/// </summary>

using Klacks.Api.Domain.Models.Scheduling;

namespace Klacks.Api.Domain.Interfaces.Scheduling;

public interface ISchedulingRuleImportRepository
{
    Task<List<SchedulingRule>> GetBySourceKeysAsync(IReadOnlyCollection<string> sourceKeys);

    void Add(SchedulingRule rule);

    void Update(SchedulingRule rule);
}
