// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Lists the holidays in a period for one holiday calendar: a named calendar selection, a country/region, or -
/// without either - the company calendar. Every row says whether the day counts as official (after the calendar's
/// "reminder only" choice) and whether working on it earns the holiday time surcharge (official and the rule is
/// marked for the time surcharge). The calculators come from the same paths payroll and the holiday-work warning use.
/// </summary>
/// <param name="calendarSelectionName">Optional. Name of a configured holiday calendar; wins over country.</param>
/// <param name="country">Optional. ISO country code; national plus regional holidays when state is given.</param>
/// <param name="state">Optional. Region code (canton, federal state).</param>
/// <param name="fromDate">Required. ISO date (yyyy-MM-dd).</param>
/// <param name="untilDate">Required. ISO date (yyyy-MM-dd), inclusive, at most five years after fromDate.</param>
/// <param name="holidayCalendarTargetResolver">Decides which calendar the question is about</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Domain.Services.Holidays;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("list_holidays_for_period")]
public class ListHolidaysForPeriodSkill : BaseSkillImplementation
{
    private const string DateFormat = "yyyy-MM-dd";
    private const int MaxSpanYears = 5;

    private const string CalendarSelectionNameParameter = "calendarSelectionName";
    private const string CountryParameter = "country";
    private const string StateParameter = "state";

    private readonly IHolidayCalendarTargetResolver _holidayCalendarTargetResolver;

    public ListHolidaysForPeriodSkill(IHolidayCalendarTargetResolver holidayCalendarTargetResolver)
    {
        _holidayCalendarTargetResolver = holidayCalendarTargetResolver;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var fromStr = GetRequiredString(parameters, "fromDate");
        var untilStr = GetRequiredString(parameters, "untilDate");

        if (!SkillCalendarStringParser.TryParseDateOnly(fromStr, context.UserLanguage, out var fromDate))
        {
            return SkillResult.Error($"Invalid fromDate: {fromStr}. Expected yyyy-MM-dd.");
        }
        if (!SkillCalendarStringParser.TryParseDateOnly(untilStr, context.UserLanguage, out var untilDate))
        {
            return SkillResult.Error($"Invalid untilDate: {untilStr}. Expected yyyy-MM-dd.");
        }
        if (untilDate < fromDate)
        {
            return SkillResult.Error("untilDate must be on or after fromDate.");
        }

        if (untilDate > fromDate.AddYears(MaxSpanYears))
        {
            return SkillResult.Error($"The period may span at most {MaxSpanYears} years - ask for a shorter one.");
        }

        var (target, error) = await _holidayCalendarTargetResolver.ResolveAsync(
            GetParameter<string>(parameters, CalendarSelectionNameParameter),
            GetParameter<string>(parameters, CountryParameter),
            GetParameter<string>(parameters, StateParameter),
            cancellationToken);
        if (target == null)
        {
            return SkillResult.Error(error ?? "The holiday calendar could not be resolved.");
        }

        var occurrences = new List<HolidayDate>();
        for (var year = fromDate.Year; year <= untilDate.Year; year++)
        {
            var calculator = await target.CalculatorForYear(year);
            if (calculator == null)
            {
                continue;
            }

            occurrences.AddRange(calculator.HolidayList.Where(h => h.CurrentDate >= fromDate && h.CurrentDate <= untilDate));
        }

        var rows = occurrences
            .OrderBy(h => h.CurrentDate)
            .Select(h => new
            {
                Date = h.CurrentDate.ToString(DateFormat),
                CurrentName = h.Name.GetValueOrFirstAvailable(context.UserLanguage),
                h.Officially,
                h.IsPaid,
                EarnsHolidayTimeSurchargeWhenWorked = h.EarnsTimeSurcharge,
                DayOfWeek = h.CurrentDate.DayOfWeek.ToString(),
                DayOfWeekLocalized = UiLanguageCulture.DayName(context.UserLanguage, h.CurrentDate.DayOfWeek)
            })
            .ToList();

        var calendarLabel = SkillMessageText.Name(target.Label) ?? "no holiday calendar configured";

        return SkillResult.SuccessResult(
            new
            {
                Calendar = target.Label,
                CalendarKind = target.Kind,
                FromDate = fromDate.ToString(DateFormat),
                UntilDate = untilDate.ToString(DateFormat),
                Holidays = rows,
                TotalCount = rows.Count
            },
            $"Found {rows.Count} holiday(s) in '{calendarLabel}' between {fromDate.ToString(DateFormat)} and {untilDate.ToString(DateFormat)}. " +
            "Officially = counts as an official holiday in this calendar; only official holidays whose rule is marked " +
            "for the time surcharge earn the holiday time surcharge when worked.");
    }
}
