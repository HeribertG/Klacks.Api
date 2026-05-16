// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Checks whether a specific date is a holiday in the given country + state. Returns the
/// holiday's name and "officially observed" flag if matched, otherwise IsHoliday=false.
/// Used before placing a Work/Break to warn about overlap with a holiday.
/// </summary>
/// <param name="date">Required. ISO date (yyyy-MM-dd).</param>
/// <param name="country">Required. ISO country code.</param>
/// <param name="state">Optional. State / canton abbreviation.</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Domain.Services.Holidays;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("validate_holiday_overlap")]
public class ValidateHolidayOverlapSkill : BaseSkillImplementation
{
    private readonly ISettingsRepository _settingsRepository;

    public ValidateHolidayOverlapSkill(ISettingsRepository settingsRepository)
    {
        _settingsRepository = settingsRepository;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var dateStr = GetRequiredString(parameters, "date");
        var country = GetRequiredString(parameters, "country").Trim().ToUpperInvariant();
        var state = (GetParameter<string>(parameters, "state") ?? string.Empty).Trim().ToUpperInvariant();

        if (!DateOnly.TryParse(dateStr, out var date))
        {
            return SkillResult.Error($"Invalid date: {dateStr}. Expected yyyy-MM-dd.");
        }

        var allRules = await _settingsRepository.GetCalendarRuleList();
        var matchingRules = allRules
            .Where(r => string.Equals(r.Country, country, StringComparison.OrdinalIgnoreCase))
            .Where(r => string.IsNullOrEmpty(state)
                ? string.IsNullOrEmpty(r.State)
                : string.Equals(r.State, state, StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(r.State))
            .ToList();

        var calculator = new HolidaysListCalculator { CurrentYear = date.Year };
        calculator.AddRange(matchingRules);
        calculator.ComputeHolidays();

        var hit = calculator.GetHolidayInfo(date);
        if (hit == null)
        {
            return SkillResult.SuccessResult(
                new
                {
                    Date = date.ToString("yyyy-MM-dd"),
                    Country = country,
                    State = state,
                    IsHoliday = false,
                    DayOfWeek = date.DayOfWeek.ToString()
                },
                $"{date:yyyy-MM-dd} is NOT a holiday in {country}/{(string.IsNullOrEmpty(state) ? "—" : state)}.");
        }

        return SkillResult.SuccessResult(
            new
            {
                Date = date.ToString("yyyy-MM-dd"),
                Country = country,
                State = state,
                IsHoliday = true,
                HolidayName = hit.CurrentName,
                hit.Officially,
                DayOfWeek = date.DayOfWeek.ToString()
            },
            $"{date:yyyy-MM-dd} is a holiday: {hit.CurrentName}" + (hit.Officially ? " (officially observed)." : "."));
    }
}
