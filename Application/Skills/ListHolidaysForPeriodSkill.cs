// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Lists holidays in the requested period for a given country + state combination. Loads all
/// matching CalendarRules from ISettingsRepository, computes occurrences via HolidaysListCalculator
/// for each covered year, and clamps the result to [fromDate..untilDate].
/// </summary>
/// <param name="country">Required. ISO country code (e.g. CH, DE, AT).</param>
/// <param name="state">Optional. State / canton abbreviation; empty matches state-less rules.</param>
/// <param name="fromDate">Required. ISO date (yyyy-MM-dd).</param>
/// <param name="untilDate">Required. ISO date (yyyy-MM-dd), inclusive.</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Domain.Services.Holidays;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("list_holidays_for_period")]
public class ListHolidaysForPeriodSkill : BaseSkillImplementation
{
    private readonly ISettingsRepository _settingsRepository;

    public ListHolidaysForPeriodSkill(ISettingsRepository settingsRepository)
    {
        _settingsRepository = settingsRepository;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var country = GetRequiredString(parameters, "country").Trim().ToUpperInvariant();
        var state = (GetParameter<string>(parameters, "state") ?? string.Empty).Trim().ToUpperInvariant();
        var fromStr = GetRequiredString(parameters, "fromDate");
        var untilStr = GetRequiredString(parameters, "untilDate");

        if (!DateOnly.TryParse(fromStr, out var fromDate))
        {
            return SkillResult.Error($"Invalid fromDate: {fromStr}. Expected yyyy-MM-dd.");
        }
        if (!DateOnly.TryParse(untilStr, out var untilDate))
        {
            return SkillResult.Error($"Invalid untilDate: {untilStr}. Expected yyyy-MM-dd.");
        }
        if (untilDate < fromDate)
        {
            return SkillResult.Error("untilDate must be on or after fromDate.");
        }

        var allRules = await _settingsRepository.GetCalendarRuleList();
        var matchingRules = allRules
            .Where(r => string.Equals(r.Country, country, StringComparison.OrdinalIgnoreCase))
            .Where(r => string.IsNullOrEmpty(state)
                ? string.IsNullOrEmpty(r.State)
                : string.Equals(r.State, state, StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(r.State))
            .ToList();

        var occurrences = new List<HolidayDate>();
        for (var year = fromDate.Year; year <= untilDate.Year; year++)
        {
            var calculator = new HolidaysListCalculator { CurrentYear = year };
            calculator.AddRange(matchingRules);
            calculator.ComputeHolidays();
            occurrences.AddRange(calculator.HolidayList.Where(h =>
                h.CurrentDate >= fromDate && h.CurrentDate <= untilDate));
        }

        var rows = occurrences
            .OrderBy(h => h.CurrentDate)
            .Select(h => new
            {
                Date = h.CurrentDate.ToString("yyyy-MM-dd"),
                h.CurrentName,
                h.Officially,
                DayOfWeek = h.CurrentDate.DayOfWeek.ToString()
            })
            .ToList();

        return SkillResult.SuccessResult(
            new
            {
                Country = country,
                State = state,
                FromDate = fromDate.ToString("yyyy-MM-dd"),
                UntilDate = untilDate.ToString("yyyy-MM-dd"),
                Holidays = rows,
                TotalCount = rows.Count
            },
            $"Found {rows.Count} holiday(s) in {country}/{(string.IsNullOrEmpty(state) ? "—" : state)} between {fromDate:yyyy-MM-dd} and {untilDate:yyyy-MM-dd}.");
    }
}
