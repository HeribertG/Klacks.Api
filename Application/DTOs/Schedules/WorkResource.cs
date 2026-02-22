// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Schedules;

public class WorkResource : ScheduleEntryResource
{
    public ShiftResource? Shift { get; set; }

    public Guid ShiftId { get; set; }
}
