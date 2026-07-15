// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Persistence access for <see cref="Klacks.Api.Domain.Models.Scheduling.RestDayRotationRule"/> rows:
/// lookup by the region-setup entity-import natural keys for the re-apply reconciliation, and the
/// active rule set used by RestDayRotationEvaluator.
/// </summary>

using Klacks.Api.Domain.Models.Scheduling;

namespace Klacks.Api.Domain.Interfaces.Scheduling;

public interface IRestDayRotationRuleRepository
{
    Task<List<RestDayRotationRule>> GetBySourceKeysAsync(IReadOnlyCollection<string> sourceKeys);

    Task<List<RestDayRotationRule>> GetAllActiveAsync();

    void Add(RestDayRotationRule rule);

    void Update(RestDayRotationRule rule);
}
