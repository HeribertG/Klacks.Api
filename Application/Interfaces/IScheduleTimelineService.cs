// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.Interfaces;

public interface IScheduleTimelineService
{
    void QueueCheck(Guid clientId, DateOnly date);
    void QueueRangeCheck(DateOnly startDate, DateOnly endDate);
}
