// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Builds the demo order definitions that both the shift seed SQL and the demo order XML export
/// are generated from, so the seeded demo plan and the exported ERP order file can never drift apart.
/// Only chains that start as an OriginalOrder record are definitions; split shifts, containers and
/// container templates are derived or technical records and are produced by the seed alone.
/// The variation inside a category (morning start hours, time range windows, root group assignment)
/// is drawn from a fixed seed, so two runs produce byte-identical definitions.
/// </summary>
/// <param name="language">Two-letter language code selecting names and descriptions</param>
/// <param name="names">Shared registry handing out collision-free names and abbreviations</param>
/// <param name="baseDate">Date every generated order starts on</param>

using System.Globalization;
using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Data.Seed.Demo;

public class DemoOrderDefinitionFactory
{
    public const string RootGroupWestSwitzerland = "Westschweiz";
    public const string RootGroupZurich = "Deutschschweiz Z\u00fcrich";
    public const string RootGroupCentral = "Deutschschweiz Mitte";
    public const string RootGroupEast = "Deutschschweiz Ost";

    public const int SimpleShiftCount = 10;
    public const int MorningShiftCount = 40;
    public const int DayShiftCount = 60;
    public const int NightShiftWeekdayCount = 40;
    public const int NightShiftWeekendCount = 40;
    public const int TwentyFourHourShiftCount = 20;
    public const int NightCutShiftCount = 10;
    public const int TimeRangeShiftsPerRootGroup = 100;

    private const int MinutesPerHour = 60;
    private const int MorningShiftHours = 6;
    private const int DayShiftHours = 8;
    private const int NightShiftHours = 8;
    private const int TwentyFourHourShiftHours = 24;
    private const int NightCutShiftHours = 8;
    private const int MorningShiftMinStartHour = 5;
    private const int MorningShiftMaxStartHourExclusive = 8;
    private const int MorningShiftDoubleStaffedUntilIndex = 3;
    private const int DayShiftDoubleStaffedUntilIndex = 5;
    private const int TwentyFourHourDoubleStaffedUntilIndex = 2;
    private const int TimeRangeMinWorkMinutes = 10;
    private const int TimeRangeMaxWorkMinutesExclusive = 31;
    private const int TimeRangeMinWindowHours = 6;
    private const int TimeRangeMaxWindowHoursExclusive = 9;
    private const int TimeRangeMidnightChancePercent = 50;
    private const int TimeRangeNightMinStartHour = 18;
    private const int TimeRangeNightMaxStartHourExclusive = 24;
    private const int TimeRangeDayMinStartHour = 6;
    private const int TimeRangeDayLatestEndHour = 19;
    private const int TimeRangeDayMinStartHourExclusiveFloor = 7;
    private const int HoursPerDay = 24;
    private const int PercentBase = 100;
    private const int MinRootGroups = 1;
    private const int MaxRootGroupsExclusive = 3;
    private const int WorkflowRootGroupsExclusive = 2;
    private const int DayShiftAbbreviationCounterOffset = 100;
    private const int WorkTimeSqlDecimals = 4;
    private const string SqlTimeFormat = "HH:mm:ss";

    private static readonly string[] AvailableRootGroups =
    [
        RootGroupWestSwitzerland,
        RootGroupZurich,
        RootGroupCentral,
        RootGroupEast
    ];

    public static readonly DateOnly DefaultBaseDate = new(2025, 1, 1);

    private static readonly TimeOnly DayShiftStart = new(8, 0);
    private static readonly TimeOnly DayShiftEnd = new(17, 0);
    private static readonly TimeOnly NightShiftStart = new(23, 0);
    private static readonly TimeOnly NightShiftEnd = new(7, 0);
    private static readonly TimeOnly TwentyFourHourShiftBoundary = new(7, 0);
    private static readonly TimeOnly NightCutStart = new(22, 0);
    private static readonly TimeOnly NightCutEnd = new(6, 0);

    private readonly string _language;
    private readonly DemoSeedNameRegistry _names;
    private readonly DateOnly _baseDate;

    public DemoOrderDefinitionFactory(string language, DemoSeedNameRegistry names, DateOnly baseDate)
    {
        _language = language;
        _names = names;
        _baseDate = baseDate;
    }

    public static IReadOnlyList<string> RootGroups => AvailableRootGroups;

    /// <summary>
    /// Builds the order definitions of the seven fixed-shift demo categories, in seed emission order.
    /// </summary>
    public IReadOnlyList<DemoOrderDefinition> CreateShiftOrders()
    {
        var random = new Random(DemoOrderSeedConstants.DemoRandomSeed);
        var definitions = new List<DemoOrderDefinition>();
        definitions.AddRange(CreateSimpleShiftOrders(random));
        definitions.AddRange(CreateMorningShiftOrders(random));
        definitions.AddRange(CreateDayShiftOrders(random));
        definitions.AddRange(CreateNightShiftWeekdayOrders(random));
        definitions.AddRange(CreateNightShiftWeekendOrders(random));
        definitions.AddRange(CreateTwentyFourHourShiftOrders(random));
        definitions.AddRange(CreateNightCutShiftOrders(random));
        return definitions;
    }

    /// <summary>
    /// Builds the time range order definitions, one hundred per root group.
    /// </summary>
    public IReadOnlyList<DemoOrderDefinition> CreateTimeRangeOrders()
    {
        var random = new Random(DemoOrderSeedConstants.DemoRandomSeed);
        var definitions = new List<DemoOrderDefinition>();

        for (var groupIndex = 0; groupIndex < AvailableRootGroups.Length; groupIndex++)
        {
            var rootGroup = AvailableRootGroups[groupIndex];

            for (var i = 1; i <= TimeRangeShiftsPerRootGroup; i++)
            {
                var workTimeMinutes = random.Next(TimeRangeMinWorkMinutes, TimeRangeMaxWorkMinutesExclusive);
                var windowHours = random.Next(TimeRangeMinWindowHours, TimeRangeMaxWindowHoursExclusive);
                var crossesMidnight = random.Next(PercentBase) < TimeRangeMidnightChancePercent;

                int startHour;
                int endHour;
                if (crossesMidnight)
                {
                    startHour = random.Next(TimeRangeNightMinStartHour, TimeRangeNightMaxStartHourExclusive);
                    endHour = (startHour + windowHours) % HoursPerDay;
                }
                else
                {
                    startHour = random.Next(TimeRangeDayMinStartHour, Math.Max(TimeRangeDayMinStartHourExclusiveFloor, TimeRangeDayLatestEndHour - windowHours));
                    endHour = startHour + windowHours;
                }

                var shiftNumber = (groupIndex * TimeRangeShiftsPerRootGroup) + i;
                var name = _language switch
                {
                    "ar" => $"\u0648\u0631\u062f\u064a\u0629 \u0632\u0645\u0646\u064a\u0629 {shiftNumber}",
                    "he" => $"\u05de\u05e9\u05de\u05e8\u05ea \u05d2\u05de\u05d9\u05e9\u05d4 {shiftNumber}",
                    "ja" => $"\u30d5\u30ec\u30c3\u30af\u30b9\u52e4\u52d9{shiftNumber}",
                    _ => $"TimeRange-Shift {shiftNumber}",
                };
                var abbreviation = _language switch
                {
                    "ar" => $"\u0648\u0632{shiftNumber}",
                    "he" => $"\u05de\u05d2{shiftNumber}",
                    "ja" => $"\u30d5\u30ec{shiftNumber}",
                    _ => $"TR{shiftNumber}",
                };

                definitions.Add(new DemoOrderDefinition
                {
                    Category = DemoOrderCategory.TimeRangeShift,
                    Index = shiftNumber,
                    Name = name,
                    Abbreviation = abbreviation,
                    Description = DemoOrderDescriptions.TimeRangeShift(_language, workTimeMinutes, windowHours, crossesMidnight),
                    OriginalShiftDescription = DemoOrderDescriptions.TimeRangeShiftGerman(workTimeMinutes, windowHours, crossesMidnight),
                    FromDate = _baseDate,
                    UntilDate = null,
                    StartShift = new TimeOnly(startHour, 0),
                    EndShift = new TimeOnly(endHour % HoursPerDay, 0),
                    WorkTimeMinutes = workTimeMinutes,
                    WorkTimeSqlLiteral = FormatWorkTime(workTimeMinutes),
                    IsTimeRange = true,
                    IsMonday = true,
                    IsTuesday = true,
                    IsWednesday = true,
                    IsThursday = true,
                    IsFriday = true,
                    RootGroups = [rootGroup]
                });
            }
        }

        return definitions;
    }

    private IEnumerable<DemoOrderDefinition> CreateSimpleShiftOrders(Random random)
    {
        var simpleShiftsAr = new[]
        {
            new { Name = "الوردية الصباحية", Abbr = "صب", Start = "07:00:00", End = "15:00:00", WorkTime = 8, Employees = 1, IsTimeRange = false },
            new { Name = "الوردية المسائية", Abbr = "مسا", Start = "15:00:00", End = "22:00:00", WorkTime = 7, Employees = 2, IsTimeRange = false },
            new { Name = "الوردية الليلية", Abbr = "ليل", Start = "23:00:00", End = "07:00:00", WorkTime = 8, Employees = 1, IsTimeRange = false },
            new { Name = "دوام نهاري", Abbr = "نهر", Start = "08:00:00", End = "16:00:00", WorkTime = 8, Employees = 1, IsTimeRange = false },
            new { Name = "الاستعداد", Abbr = "است", Start = "00:00:00", End = "00:00:00", WorkTime = 8, Employees = 1, IsTimeRange = true },
        };
        var simpleShiftsHe = new[]
        {
            new { Name = "משמרת בוקר", Abbr = "בק", Start = "07:00:00", End = "15:00:00", WorkTime = 8, Employees = 1, IsTimeRange = false },
            new { Name = "משמרת ערב", Abbr = "ער", Start = "15:00:00", End = "22:00:00", WorkTime = 7, Employees = 2, IsTimeRange = false },
            new { Name = "משמרת לילה", Abbr = "לי", Start = "23:00:00", End = "07:00:00", WorkTime = 8, Employees = 1, IsTimeRange = false },
            new { Name = "תורנות יום", Abbr = "יום", Start = "08:00:00", End = "16:00:00", WorkTime = 8, Employees = 1, IsTimeRange = false },
            new { Name = "כוננות", Abbr = "כון", Start = "00:00:00", End = "00:00:00", WorkTime = 8, Employees = 1, IsTimeRange = true },
        };
        var simpleShiftsJa = new[]
        {
            new { Name = "早番", Abbr = "早", Start = "07:00:00", End = "15:00:00", WorkTime = 8, Employees = 1, IsTimeRange = false },
            new { Name = "遅番", Abbr = "遅", Start = "15:00:00", End = "22:00:00", WorkTime = 7, Employees = 2, IsTimeRange = false },
            new { Name = "夜勤", Abbr = "夜", Start = "23:00:00", End = "07:00:00", WorkTime = 8, Employees = 1, IsTimeRange = false },
            new { Name = "日勤", Abbr = "日", Start = "08:00:00", End = "16:00:00", WorkTime = 8, Employees = 1, IsTimeRange = false },
            new { Name = "待機", Abbr = "待", Start = "00:00:00", End = "00:00:00", WorkTime = 8, Employees = 1, IsTimeRange = true },
        };
        var simpleShiftsDe = new[]
        {
            new { Name = "Frühschicht", Abbr = "FS", Start = "07:00:00", End = "15:00:00", WorkTime = 8, Employees = 1, IsTimeRange = false },
            new { Name = "Spätschicht", Abbr = "SS", Start = "15:00:00", End = "22:00:00", WorkTime = 7, Employees = 2, IsTimeRange = false },
            new { Name = "Nachtschicht", Abbr = "NS", Start = "23:00:00", End = "07:00:00", WorkTime = 8, Employees = 1, IsTimeRange = false },
            new { Name = "Tagdienst", Abbr = "TAG", Start = "08:00:00", End = "16:00:00", WorkTime = 8, Employees = 1, IsTimeRange = false },
            new { Name = "Bereitschaft", Abbr = "BD", Start = "00:00:00", End = "00:00:00", WorkTime = 8, Employees = 1, IsTimeRange = true },
        };
        var simpleShiftsBase = _language switch
        {
            "ar" => simpleShiftsAr,
            "he" => simpleShiftsHe,
            "ja" => simpleShiftsJa,
            _ => simpleShiftsDe,
        };
        var simpleShifts = simpleShiftsBase.Concat(simpleShiftsBase).ToArray();

        var index = 0;
        foreach (var shift in simpleShifts)
        {
            index++;
            var name = _names.UniqueName(shift.Name, 1);
            var abbreviation = _names.UniqueAbbreviation(shift.Abbr, 1);
            var rootGroups = PickRootGroups(random, random.Next(MinRootGroups, MaxRootGroupsExclusive));

            yield return new DemoOrderDefinition
            {
                Category = DemoOrderCategory.SimpleShift,
                Index = index,
                Name = name,
                Abbreviation = abbreviation,
                Description = DemoOrderDescriptions.SimpleShift(_language, shift.Name, shift.Employees),
                OriginalShiftDescription = DemoOrderDescriptions.SimpleShift(_language, shift.Name, shift.Employees),
                FromDate = _baseDate,
                StartShift = TimeOnly.ParseExact(shift.Start, SqlTimeFormat, CultureInfo.InvariantCulture),
                EndShift = TimeOnly.ParseExact(shift.End, SqlTimeFormat, CultureInfo.InvariantCulture),
                WorkTimeMinutes = shift.WorkTime * MinutesPerHour,
                WorkTimeSqlLiteral = shift.WorkTime.ToString(CultureInfo.InvariantCulture),
                IsTimeRange = shift.IsTimeRange,
                IsMonday = true,
                IsTuesday = true,
                IsWednesday = true,
                IsThursday = true,
                IsFriday = true,
                SumEmployees = shift.Employees,
                RootGroups = rootGroups
            };
        }
    }

    private IEnumerable<DemoOrderDefinition> CreateMorningShiftOrders(Random random)
    {
        for (var i = 1; i <= MorningShiftCount; i++)
        {
            var startHour = random.Next(MorningShiftMinStartHour, MorningShiftMaxStartHourExclusive);
            var endHour = startHour + MorningShiftHours;
            var employees = i <= MorningShiftDoubleStaffedUntilIndex ? 2 : 1;
            var name = _names.UniqueName("Morgenschicht", i);
            var abbreviation = _names.UniqueAbbreviation("MOR", i);
            var rootGroups = PickRootGroups(random, random.Next(MinRootGroups, MaxRootGroupsExclusive));
            var description = DemoOrderDescriptions.MorningShift(_language, employees);

            yield return new DemoOrderDefinition
            {
                Category = DemoOrderCategory.MorningShift,
                Index = i,
                Name = name,
                Abbreviation = abbreviation,
                Description = description,
                OriginalShiftDescription = description,
                FromDate = _baseDate,
                StartShift = new TimeOnly(startHour, 0),
                EndShift = new TimeOnly(endHour, 0),
                WorkTimeMinutes = MorningShiftHours * MinutesPerHour,
                WorkTimeSqlLiteral = MorningShiftHours.ToString(CultureInfo.InvariantCulture),
                IsMonday = true,
                IsTuesday = true,
                IsWednesday = true,
                IsThursday = true,
                IsFriday = true,
                SumEmployees = employees,
                RootGroups = rootGroups
            };
        }
    }

    private IEnumerable<DemoOrderDefinition> CreateDayShiftOrders(Random random)
    {
        for (var i = 1; i <= DayShiftCount; i++)
        {
            var employees = i <= DayShiftDoubleStaffedUntilIndex ? 2 : 1;
            var name = _names.UniqueName("Tagschicht", i);
            var abbreviation = _names.UniqueAbbreviation("TAG", i + DayShiftAbbreviationCounterOffset);
            var rootGroups = PickRootGroups(random, random.Next(MinRootGroups, MaxRootGroupsExclusive));
            var description = DemoOrderDescriptions.DayShift(_language, employees);

            yield return new DemoOrderDefinition
            {
                Category = DemoOrderCategory.DayShift,
                Index = i,
                Name = name,
                Abbreviation = abbreviation,
                Description = description,
                OriginalShiftDescription = description,
                FromDate = _baseDate,
                StartShift = DayShiftStart,
                EndShift = DayShiftEnd,
                WorkTimeMinutes = DayShiftHours * MinutesPerHour,
                WorkTimeSqlLiteral = DayShiftHours.ToString(CultureInfo.InvariantCulture),
                IsMonday = true,
                IsTuesday = true,
                IsWednesday = true,
                IsThursday = true,
                IsFriday = true,
                IsWeekdayAndHoliday = true,
                SumEmployees = employees,
                RootGroups = rootGroups
            };
        }
    }

    private IEnumerable<DemoOrderDefinition> CreateNightShiftWeekdayOrders(Random random)
    {
        for (var i = 1; i <= NightShiftWeekdayCount; i++)
        {
            var name = _names.UniqueName("Nachtdienst Mo-Fr", i);
            var abbreviation = _names.UniqueAbbreviation("NMF", i);
            var rootGroups = PickRootGroups(random, random.Next(MinRootGroups, MaxRootGroupsExclusive));
            var description = DemoOrderDescriptions.NightShiftWeekday(_language);

            yield return new DemoOrderDefinition
            {
                Category = DemoOrderCategory.NightShiftWeekday,
                Index = i,
                Name = name,
                Abbreviation = abbreviation,
                Description = description,
                OriginalShiftDescription = description,
                FromDate = _baseDate,
                StartShift = NightShiftStart,
                EndShift = NightShiftEnd,
                WorkTimeMinutes = NightShiftHours * MinutesPerHour,
                WorkTimeSqlLiteral = NightShiftHours.ToString(CultureInfo.InvariantCulture),
                IsMonday = true,
                IsTuesday = true,
                IsThursday = true,
                IsFriday = true,
                IsWeekdayAndHoliday = true,
                RootGroups = rootGroups
            };
        }
    }

    private IEnumerable<DemoOrderDefinition> CreateNightShiftWeekendOrders(Random random)
    {
        for (var i = 1; i <= NightShiftWeekendCount; i++)
        {
            var name = _names.UniqueName("Nachtdienst Sa-So", i);
            var abbreviation = _names.UniqueAbbreviation("NSS", i);
            var rootGroups = PickRootGroups(random, random.Next(MinRootGroups, MaxRootGroupsExclusive));
            var description = DemoOrderDescriptions.NightShiftWeekend(_language);

            yield return new DemoOrderDefinition
            {
                Category = DemoOrderCategory.NightShiftWeekend,
                Index = i,
                Name = name,
                Abbreviation = abbreviation,
                Description = description,
                OriginalShiftDescription = description,
                FromDate = _baseDate,
                StartShift = NightShiftStart,
                EndShift = NightShiftEnd,
                WorkTimeMinutes = NightShiftHours * MinutesPerHour,
                WorkTimeSqlLiteral = NightShiftHours.ToString(CultureInfo.InvariantCulture),
                IsSaturday = true,
                IsSunday = true,
                RootGroups = rootGroups
            };
        }
    }

    private IEnumerable<DemoOrderDefinition> CreateTwentyFourHourShiftOrders(Random random)
    {
        for (var i = 1; i <= TwentyFourHourShiftCount; i++)
        {
            var employees = i <= TwentyFourHourDoubleStaffedUntilIndex ? 2 : 1;
            var name = _names.UniqueName("24h-Schichtdienst", i);
            var abbreviation = _names.UniqueAbbreviation("24H", i);
            var rootGroups = PickRootGroups(random, random.Next(MinRootGroups, WorkflowRootGroupsExclusive));
            var description = DemoOrderDescriptions.TwentyFourHourShift(_language, employees);

            yield return new DemoOrderDefinition
            {
                Category = DemoOrderCategory.TwentyFourHourShift,
                Index = i,
                Name = name,
                Abbreviation = abbreviation,
                Description = description,
                OriginalShiftDescription = description,
                FromDate = _baseDate,
                StartShift = TwentyFourHourShiftBoundary,
                EndShift = TwentyFourHourShiftBoundary,
                WorkTimeMinutes = TwentyFourHourShiftHours * MinutesPerHour,
                WorkTimeSqlLiteral = TwentyFourHourShiftHours.ToString(CultureInfo.InvariantCulture),
                IsMonday = true,
                IsTuesday = true,
                IsWednesday = true,
                IsThursday = true,
                IsFriday = true,
                IsSaturday = true,
                IsSunday = true,
                IsHoliday = true,
                SumEmployees = employees,
                RootGroups = rootGroups
            };
        }
    }

    private IEnumerable<DemoOrderDefinition> CreateNightCutShiftOrders(Random random)
    {
        for (var i = 1; i <= NightCutShiftCount; i++)
        {
            var name = _names.UniqueName("Nachtschicht-Teilung", i);
            var abbreviation = _names.UniqueAbbreviation("NCT", i);
            var rootGroups = PickRootGroups(random, random.Next(MinRootGroups, WorkflowRootGroupsExclusive));
            var description = DemoOrderDescriptions.NightCutShift(_language);

            yield return new DemoOrderDefinition
            {
                Category = DemoOrderCategory.NightCutShift,
                Index = i,
                Name = name,
                Abbreviation = abbreviation,
                Description = description,
                OriginalShiftDescription = description,
                FromDate = _baseDate,
                StartShift = NightCutStart,
                EndShift = NightCutEnd,
                WorkTimeMinutes = NightCutShiftHours * MinutesPerHour,
                WorkTimeSqlLiteral = NightCutShiftHours.ToString(CultureInfo.InvariantCulture),
                IsMonday = true,
                IsTuesday = true,
                IsWednesday = true,
                IsThursday = true,
                IsFriday = true,
                RootGroups = rootGroups
            };
        }
    }

    private static string FormatWorkTime(int workTimeMinutes)
    {
        return Math.Round((double)workTimeMinutes / MinutesPerHour, WorkTimeSqlDecimals)
            .ToString(CultureInfo.InvariantCulture);
    }

    private static List<string> PickRootGroups(Random random, int count)
    {
        return AvailableRootGroups.OrderBy(_ => random.Next()).Take(count).ToList();
    }
}
