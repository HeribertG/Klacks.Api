// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.Interfaces;

public interface IScheduleTimelineStore
{
    void SetTimeline(Guid clientId, ClientTimeline timeline);
    void RemoveTimeline(Guid clientId);
    ClientTimeline? GetTimeline(Guid clientId);
    ScheduleBoard GetBoard();
}
