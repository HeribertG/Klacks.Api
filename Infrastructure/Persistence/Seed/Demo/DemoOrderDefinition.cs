// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Data.Seed.Demo;

public class DemoOrderDefinition
{
    private const decimal MinutesPerHour = 60m;

    private const int WorkTimeHoursDecimals = 4;

    public DemoOrderCategory Category { get; init; }

    public int Index { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Abbreviation { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string OriginalShiftDescription { get; init; } = string.Empty;

    public DateOnly FromDate { get; init; }

    public DateOnly? UntilDate { get; init; }

    public TimeOnly StartShift { get; init; }

    public TimeOnly EndShift { get; init; }

    public int WorkTimeMinutes { get; init; }

    public string WorkTimeSqlLiteral { get; init; } = string.Empty;

    public decimal WorkTimeHours => Math.Round(WorkTimeMinutes / MinutesPerHour, WorkTimeHoursDecimals);

    public bool IsTimeRange { get; init; }

    public bool IsMonday { get; init; }

    public bool IsTuesday { get; init; }

    public bool IsWednesday { get; init; }

    public bool IsThursday { get; init; }

    public bool IsFriday { get; init; }

    public bool IsSaturday { get; init; }

    public bool IsSunday { get; init; }

    public bool IsHoliday { get; init; }

    public bool IsWeekdayAndHoliday { get; init; }

    public int Quantity { get; init; } = 1;

    public int SumEmployees { get; init; } = 1;

    public IReadOnlyList<string> RootGroups { get; init; } = [];
}
