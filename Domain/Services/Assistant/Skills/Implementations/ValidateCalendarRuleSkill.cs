// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Skill that dry-runs a calendar rule grammar against a given (or the company's current) year and
/// reports the resulting date without persisting anything.
/// </summary>
/// <param name="companyClock">Resolves the company's current calendar year when no year is supplied.</param>

using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Settings;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Holidays;

namespace Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

[SkillImplementation("validate_calendar_rule")]
public class ValidateCalendarRuleSkill : BaseSkillImplementation
{
    private readonly ICompanyClock _companyClock;

    public ValidateCalendarRuleSkill(ICompanyClock companyClock)
    {
        _companyClock = companyClock;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var rule = GetParameter<string>(parameters, "rule");
        var subRule = GetParameter<string>(parameters, "subRule");
        var year = GetParameter<int?>(parameters, "year") ?? (await _companyClock.GetTodayDateAsync(cancellationToken)).Year;

        if (string.IsNullOrWhiteSpace(rule))
        {
            return SkillResult.Error("Rule cannot be empty");
        }

        try
        {
            var calculator = new HolidaysListCalculator();
            calculator.CurrentYear = year;

            var testRule = new CalendarRule
            {
                Rule = rule,
                SubRule = subRule ?? string.Empty,
                IsMandatory = true
            };

            calculator.Add(testRule);
            calculator.ComputeHolidays();

            if (calculator.HolidayList.Count > 0)
            {
                var holiday = calculator.HolidayList[0];
                var result = new
                {
                    IsValid = true,
                    Year = year,
                    Rule = rule,
                    SubRule = subRule,
                    CalculatedDate = holiday.CurrentDate.ToString("yyyy-MM-dd"),
                    FormattedDate = holiday.FormatDate,
                    DayOfWeek = holiday.CurrentDate.DayOfWeek.ToString()
                };

                return SkillResult.SuccessResult(result,
                    $"Rule '{rule}' is valid. Calculated date: {holiday.FormatDate} ({holiday.CurrentDate.DayOfWeek})");
            }

            return SkillResult.Error("Rule did not produce a valid date");
        }
        catch (Exception ex)
        {
            return SkillResult.Error($"Invalid rule format: {ex.Message}");
        }
    }
}
