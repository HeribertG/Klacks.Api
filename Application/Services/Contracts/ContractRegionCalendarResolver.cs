// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Resolves a state, canton or region code to the calendar selections that cover it in the installation's
/// default country - the same lookup list_contracts uses for its canton filter, with no countrywide fallback.
/// The raw lookup may be empty (no calendar for that region) or hold several calendars; ChooseAsync applies the
/// one decision rule both contract-writing skills share: exactly one match is taken, several matches are fine
/// only when the contract's current calendar is among them (it is kept), anything else is an error that names
/// the real calendars instead of guessing.
/// </summary>
/// <param name="countryResolver">Supplies the installation's default country code</param>
/// <param name="calendarSelectionRepository">Looks up the calendar selections for a country and state</param>
/// <param name="currentCalendarId">ChooseAsync only: the contract's current calendar selection, kept when it is one of several matches</param>
/// <param name="keptCalendarOwner">ChooseAsync only: whose calendar is kept when the region is left out, used in the error text (for example "template's")</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Settings;

namespace Klacks.Api.Application.Services.Contracts;

public static class ContractRegionCalendarResolver
{
    public static async Task<IReadOnlyList<Guid>> ResolveCalendarSelectionIdsAsync(
        ICountryResolver countryResolver,
        ICalendarSelectionRepository calendarSelectionRepository,
        string region,
        CancellationToken cancellationToken)
    {
        var defaultCountry = await countryResolver.GetDefaultAsync(cancellationToken);
        var countryCode = defaultCountry?.Abbreviation ?? string.Empty;

        return await calendarSelectionRepository.GetIdsByStateAsync(
            countryCode, region.Trim().ToUpperInvariant(), cancellationToken);
    }

    public static async Task<(Guid? CalendarSelectionId, string? Error)> ChooseAsync(
        ICountryResolver countryResolver,
        ICalendarSelectionRepository calendarSelectionRepository,
        string region,
        Guid? currentCalendarId,
        string keptCalendarOwner,
        CancellationToken cancellationToken)
    {
        var ids = await ResolveCalendarSelectionIdsAsync(
            countryResolver, calendarSelectionRepository, region, cancellationToken);

        if (ids.Count == 1)
        {
            return (ids[0], null);
        }

        if (ids.Count > 1 && currentCalendarId is { } currentId && ids.Contains(currentId))
        {
            return (currentId, null);
        }

        var calendars = await calendarSelectionRepository.List();

        if (ids.Count == 0)
        {
            var existing = calendars.Count > 0 ? string.Join(", ", calendars.Select(c => c.Name)) : "none";
            return (null, $"No holiday calendar is configured for region '{region}'. Existing calendars: {existing}. " +
                          $"Ask the administrator which calendar is meant or leave '{ContractFieldNames.Region}' out to keep the " +
                          $"{keptCalendarOwner} calendar.");
        }

        var matching = string.Join(", ", calendars.Where(c => ids.Contains(c.Id)).Select(c => c.Name));
        return (null, $"Region '{region}' matches several holiday calendars: {matching}. Ask the administrator which one " +
                      "is meant — do not guess.");
    }
}
