// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Default <see cref="IHolidayCalendarLookupService"/>. Selects the calendar rules of the national entry
/// (country code as region, the notation every seeded calendar selection uses) and of the requested region once and
/// runs them through the same HolidaysListCalculator the payroll path uses, one calculator per requested year.
/// </summary>
/// <param name="settingsRepository">Reads all calendar rules</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Domain.Interfaces.CalendarSelections;
using Klacks.Api.Domain.Services.Holidays;

namespace Klacks.Api.Application.Services.Schedules;

public sealed class HolidayCalendarLookupService : IHolidayCalendarLookupService
{
    private readonly ISettingsRepository _settingsRepository;

    public HolidayCalendarLookupService(ISettingsRepository settingsRepository)
    {
        _settingsRepository = settingsRepository;
    }

    public async Task<Func<int, IHolidaysListCalculator>> PrepareAsync(string country, string? state)
    {
        var region = string.IsNullOrWhiteSpace(state) ? country : state.Trim();
        var rules = (await _settingsRepository.GetCalendarRuleList())
            .Where(r => string.Equals(r.Country, country, StringComparison.OrdinalIgnoreCase))
            .Where(r => string.Equals(r.State, country, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(r.State, region, StringComparison.OrdinalIgnoreCase))
            .ToList();

        return year =>
        {
            var calculator = new HolidaysListCalculator { CurrentYear = year };
            calculator.AddRange(rules);
            calculator.ComputeHolidays();
            return calculator;
        };
    }
}
