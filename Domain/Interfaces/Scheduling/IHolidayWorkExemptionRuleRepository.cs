// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Persistence access for <see cref="Klacks.Api.Domain.Models.Scheduling.HolidayWorkExemptionRule"/>
/// rows: the active set used by the holiday-work detector, plus lookup by the region-setup
/// entity-import natural keys for the re-apply reconciliation.
/// </summary>

using Klacks.Api.Domain.Models.Scheduling;

namespace Klacks.Api.Domain.Interfaces.Scheduling;

public interface IHolidayWorkExemptionRuleRepository
{
    Task<List<HolidayWorkExemptionRule>> GetAllActiveAsync();

    Task<List<HolidayWorkExemptionRule>> GetBySourceKeysAsync(IReadOnlyCollection<string> sourceKeys);

    void Add(HolidayWorkExemptionRule rule);

    void Update(HolidayWorkExemptionRule rule);
}
