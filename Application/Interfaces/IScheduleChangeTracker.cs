// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.Interfaces;

public interface IScheduleChangeTracker
{
    Task TrackChangeAsync(Guid clientId, DateOnly changeDate, Guid? analyseToken);
    Task<List<ScheduleChange>> GetChangesAsync(DateOnly startDate, DateOnly endDate);
    Task ClearChangesAsync(Guid clientId, DateOnly startDate, DateOnly endDate);
}
