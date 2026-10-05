// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Default <see cref="IHolidayCalendarSourceResolver"/>. Mirrors the decision order of ClientHolidayCalendarResolver
/// (contract selection, company default selection, company country/region pair when both are set, none) so the
/// assistant can name the calendar it explains; HolidayCalendarSourceConsistencyTests pins the two together.
/// </summary>
/// <param name="settingsReader">Reads the company calendar settings</param>
/// <param name="calendarSelectionRepository">Reads the display name of a calendar selection</param>

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Settings;

namespace Klacks.Api.Application.Services.Schedules;

public sealed class HolidayCalendarSourceResolver : IHolidayCalendarSourceResolver
{
    private static readonly string[] CompanyCalendarKeys =
    [
        SettingKeys.GlobalCalendarSelectionId,
        SettingKeys.GlobalCalendarCountry,
        SettingKeys.GlobalCalendarState,
    ];

    private readonly ISettingsReader _settingsReader;
    private readonly ICalendarSelectionRepository _calendarSelectionRepository;

    public HolidayCalendarSourceResolver(
        ISettingsReader settingsReader,
        ICalendarSelectionRepository calendarSelectionRepository)
    {
        _settingsReader = settingsReader;
        _calendarSelectionRepository = calendarSelectionRepository;
    }

    public async Task<ResolvedHolidayCalendarSource> ResolveAsync(
        Guid? contractCalendarSelectionId,
        CancellationToken cancellationToken = default)
    {
        if (contractCalendarSelectionId.HasValue)
        {
            return await ForSelectionAsync(HolidayCalendarSource.Contract, contractCalendarSelectionId.Value);
        }

        var settings = await _settingsReader.GetSettingsByTypesAsync(CompanyCalendarKeys, cancellationToken);

        if (settings.TryGetValue(SettingKeys.GlobalCalendarSelectionId, out var rawId)
            && Guid.TryParse(rawId, out var companySelectionId))
        {
            return await ForSelectionAsync(HolidayCalendarSource.CompanyDefault, companySelectionId);
        }

        var country = settings.GetValueOrDefault(SettingKeys.GlobalCalendarCountry);
        var state = settings.GetValueOrDefault(SettingKeys.GlobalCalendarState);
        if (!string.IsNullOrEmpty(country) && !string.IsNullOrEmpty(state))
        {
            return new ResolvedHolidayCalendarSource(HolidayCalendarSource.CompanyCountryState, null, null, country, state);
        }

        return new ResolvedHolidayCalendarSource(HolidayCalendarSource.None, null, null, null, null);
    }

    private async Task<ResolvedHolidayCalendarSource> ForSelectionAsync(HolidayCalendarSource source, Guid selectionId)
    {
        var selection = await _calendarSelectionRepository.GetNoTracking(selectionId);
        return new ResolvedHolidayCalendarSource(source, selectionId, selection?.Name, null, null);
    }
}
