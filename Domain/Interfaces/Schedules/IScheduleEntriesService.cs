// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Interfaces.Schedules;

public interface IScheduleEntriesService
{
    IQueryable<ScheduleCell> GetScheduleEntriesQuery(
        DateOnly startDate,
        DateOnly endDate,
        List<Guid>? visibleGroupIds = null);
}
