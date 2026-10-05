// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Checks whether one date is a holiday in one holiday calendar: a named calendar selection, a country/region, or -
/// without either - the company calendar. Reports whether the day counts as official (after the calendar's
/// "reminder only" choice), so working on it raises the holiday-work warning, and whether working on it earns the
/// holiday time surcharge (official and the rule is marked for the time surcharge). The calculator comes from the
/// same paths payroll and the holiday-work warning use.
/// </summary>
/// <param name="date">Required. ISO date (yyyy-MM-dd).</param>
/// <param name="calendarSelectionName">Optional. Name of a configured holiday calendar; wins over country.</param>
/// <param name="country">Optional. ISO country code; national plus regional holidays when state is given.</param>
/// <param name="state">Optional. Region code (canton, federal state).</param>
/// <param name="holidayCalendarTargetResolver">Decides which calendar the question is about</param>

using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("validate_holiday_overlap")]
public class ValidateHolidayOverlapSkill : BaseSkillImplementation
{
    private const string DateFormat = "yyyy-MM-dd";

    private const string CalendarSelectionNameParameter = "calendarSelectionName";
    private const string CountryParameter = "country";
    private const string StateParameter = "state";

    private readonly IHolidayCalendarTargetResolver _holidayCalendarTargetResolver;

    public ValidateHolidayOverlapSkill(IHolidayCalendarTargetResolver holidayCalendarTargetResolver)
    {
        _holidayCalendarTargetResolver = holidayCalendarTargetResolver;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var dateStr = GetRequiredString(parameters, "date");
        if (!SkillCalendarStringParser.TryParseDateOnly(dateStr, context.UserLanguage, out var date))
        {
            return SkillResult.Error($"Invalid date: {dateStr}. Expected yyyy-MM-dd.");
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

        var calendarLabel = SkillMessageText.Name(target.Label) ?? "no holiday calendar configured";
        var calculator = await target.CalculatorForYear(date.Year);
        var hit = calculator?.GetHolidayInfo(date);
        if (calculator == null || hit == null)
        {
            return SkillResult.SuccessResult(
                new
                {
                    Date = date.ToString(DateFormat),
                    Calendar = target.Label,
                    CalendarKind = target.Kind,
                    IsHoliday = false,
                    DayOfWeek = date.DayOfWeek.ToString(),
                    DayOfWeekLocalized = UiLanguageCulture.DayName(context.UserLanguage, date.DayOfWeek)
                },
                $"{date.ToString(DateFormat)} is NOT a holiday in '{calendarLabel}'.");
        }

        var holidayName = hit.Name.GetValueOrFirstAvailable(context.UserLanguage);
        var isOfficial = calculator.IsHoliday(date) == HolidayStatus.OfficialHoliday;
        var earnsSurcharge = calculator.IsPaidOfficialHoliday(date);

        return SkillResult.SuccessResult(
            new
            {
                Date = date.ToString(DateFormat),
                Calendar = target.Label,
                CalendarKind = target.Kind,
                IsHoliday = true,
                HolidayName = holidayName,
                Officially = isOfficial,
                RaisesHolidayWorkWarningWhenWorked = isOfficial,
                EarnsHolidayTimeSurchargeWhenWorked = earnsSurcharge,
                DayOfWeek = date.DayOfWeek.ToString(),
                DayOfWeekLocalized = UiLanguageCulture.DayName(context.UserLanguage, date.DayOfWeek)
            },
            $"{date.ToString(DateFormat)} is a holiday in '{calendarLabel}': {SkillMessageText.Name(holidayName)}" +
            (isOfficial ? " (official)" : " (not official - shown as a reminder only)") +
            (earnsSurcharge ? "; working on it earns the holiday time surcharge." : "; working on it earns no holiday time surcharge."));
    }
}
