// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Scheduling;

public class RestrictedTimeWindowRuleResource
{
    public Guid Id { get; set; }

    public int SeasonFromMonth { get; set; }

    public int SeasonFromDay { get; set; }

    public int SeasonToMonth { get; set; }

    public int SeasonToDay { get; set; }

    public TimeOnly DailyStart { get; set; }

    public TimeOnly DailyEnd { get; set; }

    public string AppliesToGroupTag { get; set; } = string.Empty;
}
