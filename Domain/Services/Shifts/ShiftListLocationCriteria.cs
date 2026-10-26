// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Adds the multi-select location lists of the shift list filter (countries and country-bound states picked in the
/// dropdowns) to the criteria resolved from the text fields. Country codes are trimmed, upper-cased and de-duplicated;
/// states become country-bound StateCodeMatch pairs (a blank country = any country); blank entries are dropped. A state
/// from the list is accepted in addition to a state resolved from the text field (states are alternatives), while
/// the country list and the text country must both hold. Empty or missing lists leave the criteria unchanged.
/// </summary>
/// <param name="resolved">Criteria resolved from the text fields (country, state, city, zip prefix)</param>
/// <param name="filter">The shift list filter carrying the Countries and States lists</param>

using Klacks.Api.Domain.DTOs.Filter;
using Klacks.Api.Domain.Models.Geo;

namespace Klacks.Api.Domain.Services.Shifts;

public static class ShiftListLocationCriteria
{
    public static AddressLocationCriteria Extend(AddressLocationCriteria resolved, ShiftFilter filter)
    {
        var countryCodes = (filter.Countries ?? [])
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Select(c => c.Trim().ToUpperInvariant())
            .Distinct()
            .ToList();

        var listStates = (filter.States ?? [])
            .Where(s => !string.IsNullOrWhiteSpace(s?.State))
            .Select(s => new StateCodeMatch(
                s.State.Trim(), string.IsNullOrWhiteSpace(s.Country) ? null : s.Country.Trim()))
            .Distinct()
            .ToList();

        if (countryCodes.Count == 0 && listStates.Count == 0)
        {
            return resolved;
        }

        return resolved with
        {
            CountryCodes = countryCodes,
            States = resolved.States.Concat(listStates).ToList()
        };
    }
}
