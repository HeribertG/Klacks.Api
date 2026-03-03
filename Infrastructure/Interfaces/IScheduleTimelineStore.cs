// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Infrastructure.Interfaces;

public interface IScheduleTimelineStore
{
    void SetTimeline(Guid clientId, DateOnly date, ClientDayTimeline timeline);
    void RemoveTimeline(Guid clientId, DateOnly date);
    ClientDayTimeline? GetTimeline(Guid clientId, DateOnly date);
    List<ClientDayTimeline> GetTimelinesForDateRange(DateOnly startDate, DateOnly endDate);
}
