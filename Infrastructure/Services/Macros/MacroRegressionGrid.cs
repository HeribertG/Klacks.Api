// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// The fixed grid of test inputs on which an original macro and its extended copy are compared: day, late,
/// midnight-crossing and full-day time windows on every ISO weekday, with and without a holiday today or
/// tomorrow, two contract profiles (no guaranteed hours / part-time), two surcharge-rate sets with different
/// rankings and two weekend configurations (two days with the third slot unused, and three days so the third
/// weekend surcharge is reached). A grid is a sample, not a proof: branches no input reaches stay unchecked.
/// </summary>

using System.Globalization;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Models.Macros;

namespace Klacks.Api.Infrastructure.Services.Macros;

public static class MacroRegressionGrid
{
    private const int FirstIsoWeekday = 1;
    private const int DaysPerWeek = 7;
    private const int UnusedWeekendDay = 0;
    private const int RateSetNumberOffset = 1;

    private const string DescriptionFormat =
        "weekday {0}, {1}-{2}, holiday {3}, holiday next day {4}, guaranteed hours {5}, workload {6}%, "
        + "rate set {7}, weekend days {8}/{9}/{10}";

    private static readonly (string From, string Until, decimal Hours)[] TimeWindows =
    [
        ("07:00", "15:00", 8m),
        ("15:00", "23:00", 8m),
        ("22:00", "06:00", 8m),
        ("00:00", "00:00", 8.4m)
    ];

    private static readonly (bool Today, bool NextDay)[] HolidayFlags =
    [
        (false, false),
        (true, false),
        (false, true)
    ];

    private static readonly (decimal GuaranteedHours, decimal FullTime, decimal WorkloadPercent)[] ContractProfiles =
    [
        (0m, 42m, 100m),
        (160m, 42m, 60m)
    ];

    private static readonly (decimal Night, decimal Holiday, decimal We1, decimal We2, decimal We3)[] RateProfiles =
    [
        (0.10m, 0.50m, 0.25m, 0.50m, 0.30m),
        (0.50m, 0.20m, 0.30m, 0.25m, 0.40m)
    ];

    private static readonly (int First, int Second, int Third)[] WeekendProfiles =
    [
        (6, 7, UnusedWeekendDay),
        (5, 6, 7)
    ];

    public static IReadOnlyList<MacroRegressionSample> Samples { get; } = Build();

    private static IReadOnlyList<MacroRegressionSample> Build() =>
        (from window in TimeWindows
         from weekday in Enumerable.Range(FirstIsoWeekday, DaysPerWeek)
         from holiday in HolidayFlags
         from contract in ContractProfiles
         from rateIndex in Enumerable.Range(0, RateProfiles.Length)
         from weekend in WeekendProfiles
         select CreateSample(window, weekday, holiday, contract, rateIndex, weekend))
        .ToList();

    private static MacroRegressionSample CreateSample(
        (string From, string Until, decimal Hours) window,
        int weekday,
        (bool Today, bool NextDay) holiday,
        (decimal GuaranteedHours, decimal FullTime, decimal WorkloadPercent) contract,
        int rateIndex,
        (int First, int Second, int Third) weekend)
    {
        var rates = RateProfiles[rateIndex];
        var data = new MacroData
        {
            Hour = window.Hours,
            FromHour = window.From,
            UntilHour = window.Until,
            Weekday = weekday,
            Holiday = holiday.Today,
            HolidayNextDay = holiday.NextDay,
            NightRate = rates.Night,
            HolidayRate = rates.Holiday,
            WE1Rate = rates.We1,
            WE2Rate = rates.We2,
            WE3Rate = rates.We3,
            NightStart = SurchargeDefaults.NightStart,
            NightEnd = SurchargeDefaults.NightEnd,
            GuaranteedHours = contract.GuaranteedHours,
            FullTime = contract.FullTime,
            WorkloadPercent = contract.WorkloadPercent,
            WeekendDay1 = weekend.First,
            WeekendDay2 = weekend.Second,
            WeekendDay3 = weekend.Third
        };

        var description = string.Format(
            CultureInfo.InvariantCulture,
            DescriptionFormat,
            weekday,
            window.From,
            window.Until,
            holiday.Today,
            holiday.NextDay,
            contract.GuaranteedHours,
            contract.WorkloadPercent,
            rateIndex + RateSetNumberOffset,
            weekend.First,
            weekend.Second,
            weekend.Third);

        return new MacroRegressionSample(description, data);
    }
}
