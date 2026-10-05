// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Default <see cref="IHolidayCalendarTargetResolver"/>, always through the production paths: a named calendar
/// selection and the company calendar go through IClientHolidayCalendarResolver (selection entries, "reminder only"
/// override and paid flag exactly as payroll and the holiday-work warning see them); a bare country/region goes
/// through IHolidayCalendarLookupService (national plus regional entries, rules loaded once for all years).
/// </summary>
/// <param name="calendarSelectionRepository">Candidate calendar selections for name resolution</param>
/// <param name="holidayCalendarResolver">Production calculator per selection and year</param>
/// <param name="holidayCalendarSourceResolver">Names the company calendar</param>
/// <param name="holidayCalendarLookupService">Country/region calendars</param>

using Klacks.Api.Application.Constants;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Application.Skills;
using Klacks.Api.Domain.Interfaces.CalendarSelections;

namespace Klacks.Api.Application.Services.Schedules;

public sealed class HolidayCalendarTargetResolver : IHolidayCalendarTargetResolver
{
    private readonly ICalendarSelectionRepository _calendarSelectionRepository;
    private readonly IClientHolidayCalendarResolver _holidayCalendarResolver;
    private readonly IHolidayCalendarSourceResolver _holidayCalendarSourceResolver;
    private readonly IHolidayCalendarLookupService _holidayCalendarLookupService;

    public HolidayCalendarTargetResolver(
        ICalendarSelectionRepository calendarSelectionRepository,
        IClientHolidayCalendarResolver holidayCalendarResolver,
        IHolidayCalendarSourceResolver holidayCalendarSourceResolver,
        IHolidayCalendarLookupService holidayCalendarLookupService)
    {
        _calendarSelectionRepository = calendarSelectionRepository;
        _holidayCalendarResolver = holidayCalendarResolver;
        _holidayCalendarSourceResolver = holidayCalendarSourceResolver;
        _holidayCalendarLookupService = holidayCalendarLookupService;
    }

    public async Task<(HolidayCalendarTarget? Target, string? Error)> ResolveAsync(
        string? calendarSelectionName,
        string? country,
        string? state,
        CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(calendarSelectionName))
        {
            var selections = await _calendarSelectionRepository.List();
            var (selection, error) = CalendarSelectionResolver.Resolve(selections, calendarSelectionName);
            if (selection == null)
            {
                return (null, error);
            }

            return (new HolidayCalendarTarget(
                selection.Name,
                HolidayCalendarTargetKinds.CalendarSelection,
                year => _holidayCalendarResolver.GetCalculatorAsync(selection.Id, year)), null);
        }

        if (!string.IsNullOrWhiteSpace(country))
        {
            var normalizedCountry = country.Trim().ToUpperInvariant();
            var normalizedState = (state ?? string.Empty).Trim().ToUpperInvariant();
            var label = normalizedState.Length == 0
                ? normalizedCountry
                : CalendarTokenParser.Format(normalizedCountry, normalizedState);
            var calculatorFor = await _holidayCalendarLookupService.PrepareAsync(normalizedCountry, normalizedState);

            return (new HolidayCalendarTarget(
                label,
                HolidayCalendarTargetKinds.CountryRegion,
                year => Task.FromResult<IHolidaysListCalculator?>(calculatorFor(year))), null);
        }

        var company = await _holidayCalendarSourceResolver.ResolveAsync(null, cancellationToken);
        return (new HolidayCalendarTarget(
            HolidayCalendarDisplayName.Of(company),
            HolidayCalendarTargetKinds.CompanyCalendar,
            year => _holidayCalendarResolver.GetCalculatorAsync(null, year)), null);
    }
}
