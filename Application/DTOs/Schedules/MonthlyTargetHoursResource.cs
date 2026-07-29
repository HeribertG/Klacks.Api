// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Schedules;

public class MonthlyTargetHoursResource
{
    public Guid Id { get; set; }

    public int Year { get; set; }

    public int Month { get; set; }

    public decimal Hours { get; set; }
}
