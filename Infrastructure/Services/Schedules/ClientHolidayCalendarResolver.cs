// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Default <see cref="IClientHolidayCalendarResolver"/>. Lifted verbatim out of MacroDataProvider,
/// which had been the only place that knew the resolution order, so the surcharge path and the
/// holiday-work detector now read the same answer instead of each carrying their own copy.
/// </summary>
/// <param name="context">Reads calendar rules, selected calendars and the global calendar settings</param>
/// <param name="holidayCache">Caches a computed calculator per calendar selection and year</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.CalendarSelections;
using Klacks.Api.Domain.Services.Holidays;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.Schedules;

public class ClientHolidayCalendarResolver : IClientHolidayCalendarResolver
{
    private readonly DataBaseContext _context;
    private readonly IHolidayCalculatorCache _holidayCache;

    public ClientHolidayCalendarResolver(DataBaseContext context, IHolidayCalculatorCache holidayCache)
    {
        _context = context;
        _holidayCache = holidayCache;
    }

    public async Task<IHolidaysListCalculator?> GetCalculatorAsync(Guid? calendarSelectionId, int year)
    {
        if (calendarSelectionId != null)
        {
            return await GetForSelectionAsync(calendarSelectionId.Value, year);
        }

        var globalSettings = await GetGlobalCalendarSettingsAsync();
        if (globalSettings.CalendarSelectionId != null)
        {
            return await GetForSelectionAsync(globalSettings.CalendarSelectionId.Value, year);
        }

        if (!string.IsNullOrEmpty(globalSettings.Country) && !string.IsNullOrEmpty(globalSettings.State))
        {
            return await GetCalculatorByCountryStateAsync(globalSettings.Country, globalSettings.State, year);
        }

        return null;
    }

    private async Task<(string? Country, string? State, Guid? CalendarSelectionId)> GetGlobalCalendarSettingsAsync()
    {
        var settingTypes = new[] { SettingKeys.GlobalCalendarCountry, SettingKeys.GlobalCalendarState, SettingKeys.GlobalCalendarSelectionId };
        var settings = await _context.Settings
            .Where(s => settingTypes.Contains(s.Type))
            .ToListAsync();

        var country = settings.FirstOrDefault(s => s.Type == SettingKeys.GlobalCalendarCountry)?.Value;
        var state = settings.FirstOrDefault(s => s.Type == SettingKeys.GlobalCalendarState)?.Value;
        var calendarSelectionIdStr = settings.FirstOrDefault(s => s.Type == SettingKeys.GlobalCalendarSelectionId)?.Value;

        Guid? calendarSelectionId = null;
        if (!string.IsNullOrEmpty(calendarSelectionIdStr) && Guid.TryParse(calendarSelectionIdStr, out var parsedGuid))
        {
            calendarSelectionId = parsedGuid;
        }

        return (country, state, calendarSelectionId);
    }

    private async Task<IHolidaysListCalculator> GetCalculatorByCountryStateAsync(string country, string state, int year)
    {
        var calculator = new HolidaysListCalculator { CurrentYear = year };
        var rules = await LoadCalendarRulesByCountryState(country, state);
        calculator.AddRange(rules);
        calculator.ComputeHolidays();
        return calculator;
    }

    private async Task<List<Domain.Models.Settings.CalendarRule>> LoadCalendarRulesByCountryState(string country, string state)
    {
        return await _context.CalendarRule
            .Where(cr => cr.Country == country && cr.State == state)
            .ToListAsync();
    }

    private async Task<IHolidaysListCalculator> GetForSelectionAsync(Guid calendarSelectionId, int year)
    {
        return await _holidayCache.GetOrCreateAsync(calendarSelectionId, year, async () =>
        {
            var calculator = new HolidaysListCalculator { CurrentYear = year };
            var rules = await LoadCalendarRulesForSelection(calendarSelectionId);
            calculator.AddRange(rules);
            calculator.ComputeHolidays();
            return calculator;
        });
    }

    private async Task<List<Domain.Models.Settings.CalendarRule>> LoadCalendarRulesForSelection(Guid calendarSelectionId)
    {
        var selectedCalendars = await _context.Set<Domain.Models.CalendarSelections.SelectedCalendar>()
            .Where(sc => sc.CalendarSelectionId == calendarSelectionId)
            .Select(sc => new { sc.Country, sc.State, sc.OfficialOverride })
            .ToListAsync();

        if (selectedCalendars.Count == 0)
        {
            return new List<Domain.Models.Settings.CalendarRule>();
        }

        var countries = selectedCalendars.Select(sc => sc.Country).Distinct().ToList();
        var states = selectedCalendars.Select(sc => sc.State).Distinct().ToList();

        var rules = await _context.CalendarRule
            .AsNoTracking()
            .Where(cr => countries.Contains(cr.Country) && states.Contains(cr.State))
            .ToListAsync();

        var effectiveRules = new List<Domain.Models.Settings.CalendarRule>();
        foreach (var rule in rules)
        {
            var selectedCalendar = selectedCalendars
                .FirstOrDefault(sc => sc.Country == rule.Country && sc.State == rule.State);

            if (selectedCalendar == null)
            {
                continue;
            }

            var effectiveOfficial = selectedCalendar.OfficialOverride ?? rule.IsMandatory;
            effectiveRules.Add(ProjectWithEffectiveOfficial(rule, effectiveOfficial));
        }

        return effectiveRules;
    }

    private static Domain.Models.Settings.CalendarRule ProjectWithEffectiveOfficial(
        Domain.Models.Settings.CalendarRule source,
        bool effectiveOfficial)
    {
        return new Domain.Models.Settings.CalendarRule
        {
            Id = source.Id,
            Country = source.Country,
            State = source.State,
            Name = source.Name,
            Description = source.Description,
            Rule = source.Rule,
            SubRule = source.SubRule,
            IsPaid = source.IsPaid,
            IsMandatory = effectiveOfficial
        };
    }
}
