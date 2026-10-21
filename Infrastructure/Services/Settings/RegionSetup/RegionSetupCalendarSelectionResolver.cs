// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Resolves locale.calendarSelection of a region-setup profile to the id of an existing global calendar
/// selection: the state entry first, then the countrywide entry; several matches pick the lowest id
/// deterministically, no match fails the setup.
/// </summary>
/// <param name="calendarSelectionRepository">Looks up calendar selection ids by country and state</param>
/// <param name="logger">Logger instance for diagnostic output</param>

using Klacks.Api.Application.DTOs.Setup;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Exceptions;

namespace Klacks.Api.Infrastructure.Services.Settings.RegionSetup;

public sealed class RegionSetupCalendarSelectionResolver
{
    private readonly ICalendarSelectionRepository _calendarSelectionRepository;
    private readonly ILogger<RegionSetupCalendarSelectionResolver> _logger;

    public RegionSetupCalendarSelectionResolver(
        ICalendarSelectionRepository calendarSelectionRepository,
        ILogger<RegionSetupCalendarSelectionResolver> logger)
    {
        _calendarSelectionRepository = calendarSelectionRepository;
        _logger = logger;
    }

    public async Task<Guid?> ResolveCalendarSelectionIdAsync(RegionSetupCalendarSelection? calendarSelection)
    {
        if (calendarSelection == null)
        {
            return null;
        }

        var country = calendarSelection.Country!.Trim();
        var state = calendarSelection.State?.Trim() ?? string.Empty;
        var countrywideState = country;

        var ids = state.Length > 0
            ? await _calendarSelectionRepository.GetIdsByStateAsync(country, state)
            : new List<Guid>();

        if (ids.Count == 0)
        {
            ids = await _calendarSelectionRepository.GetIdsByStateAsync(country, countrywideState);
        }

        if (ids.Count == 0)
        {
            throw new InvalidRequestException(
                $"Region setup: no calendar selection found for country '{country}' and state '{state}'. " +
                "Install the matching language plugin or create a calendar selection for this region first.");
        }

        if (ids.Count > 1)
        {
            _logger.LogWarning(
                "Region setup: {Count} calendar selections match country '{Country}' and state '{State}', using the first one deterministically",
                ids.Count,
                country,
                state);
        }

        return ids.OrderBy(id => id).First();
    }
}
