// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Scheduling;

namespace Klacks.Api.Application.Interfaces;

public interface ISchedulingRuleRepository : IBaseRepository<SchedulingRule>
{
    Task<List<SchedulingRule>> GetSelectableAsync(IReadOnlyCollection<string> activeIndustrySlugs);

    Task<List<SchedulingRule>> GetByIndustryAsync(string industry);

    Task<int> GetCustomRuleCountAsync();
}
