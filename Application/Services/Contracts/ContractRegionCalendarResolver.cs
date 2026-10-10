// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Resolves a state, canton or region code to the calendar selections that cover it in the installation's
/// default country - the same lookup list_contracts uses for its canton filter, with no countrywide fallback.
/// The result may be empty (no calendar for that region) or hold several calendars; the callers decide what
/// each case means.
/// </summary>
/// <param name="countryResolver">Supplies the installation's default country code</param>
/// <param name="calendarSelectionRepository">Looks up the calendar selections for a country and state</param>

using Klacks.Api.Application.Interfaces;
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
}
