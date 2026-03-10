// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Settings;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Holidays;

namespace Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

[SkillImplementation("validate_calendar_rule")]
public class ValidateCalendarRuleSkill : BaseSkillImplementation
{
    public override Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var rule = GetParameter<string>(parameters, "rule");
        var subRule = GetParameter<string>(parameters, "subRule");
        var year = GetParameter<int?>(parameters, "year") ?? DateTime.Now.Year;

        if (string.IsNullOrWhiteSpace(rule))
        {
            return Task.FromResult(SkillResult.Error("Rule cannot be empty"));
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

                return Task.FromResult(SkillResult.SuccessResult(result,
                    $"Rule '{rule}' is valid. Calculated date: {holiday.FormatDate} ({holiday.CurrentDate.DayOfWeek})"));
            }

            return Task.FromResult(SkillResult.Error("Rule did not produce a valid date"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(SkillResult.Error($"Invalid rule format: {ex.Message}"));
        }
    }
}
