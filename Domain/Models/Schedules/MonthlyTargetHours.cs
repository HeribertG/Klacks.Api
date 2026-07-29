// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Common;

namespace Klacks.Api.Domain.Models.Schedules;

public class MonthlyTargetHours : BaseEntity
{
    public int Year { get; set; }

    public int Month { get; set; }

    public decimal Hours { get; set; }
}
