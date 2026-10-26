// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The one location predicate on an address, shared by the client search (any address of the person) and the order
/// location filter (the address OrderLocationAddressRule picks): not deleted, and every set criterion holds on this
/// address - country code, one of the states (a country-bound state also accepts an address without a country),
/// city (case-insensitive equality) and zip prefix (case-insensitive). Written as one lambda without invocations;
/// AnyOf splices it into Enumerable.Any over an owner's address sequence, so it translates as a correlated subquery
/// and the same tree also runs in memory.
/// </summary>
/// <param name="criteria">Resolved location criteria; an empty criteria set only excludes deleted addresses</param>
/// <param name="addresses">The owner's address sequence the predicate is tested on (all addresses, or a selection)</param>

using System.Linq.Expressions;
using Klacks.Api.Domain.Models.Geo;
using Klacks.Api.Domain.Models.Staffs;

namespace Klacks.Api.Domain.Services.Geo;

public static class AddressLocationPredicate
{
    private const string StateCountryKeySeparator = "|";

    public static Expression<Func<Address, bool>> Matches(AddressLocationCriteria criteria)
    {
        var countryCode = Normalize(criteria.CountryCode)?.ToUpperInvariant();
        var hasCountry = countryCode != null;

        var countryCodes = (criteria.CountryCodes ?? [])
            .Select(Normalize)
            .OfType<string>()
            .Select(c => c.ToUpperInvariant())
            .Distinct()
            .ToList();
        var hasCountryCodes = countryCodes.Count > 0;

        var hasState = criteria.States.Count > 0;
        var anyCountryStateCodes = criteria.States
            .Where(s => string.IsNullOrWhiteSpace(s.CountryCode))
            .Select(s => s.Code.Trim().ToUpperInvariant())
            .Distinct()
            .ToList();
        var countryBound = criteria.States
            .Where(s => !string.IsNullOrWhiteSpace(s.CountryCode))
            .ToList();
        var countryBoundStateCodes = countryBound
            .Select(s => s.Code.Trim().ToUpperInvariant())
            .Distinct()
            .ToList();
        var stateCountryKeys = countryBound
            .Select(s => StateCountryKey(s.Code, s.CountryCode!))
            .Distinct()
            .ToList();

        var city = Normalize(criteria.City);
        var hasCity = city != null;
        var cityUpper = (city ?? string.Empty).ToUpperInvariant();

        var zipPrefix = Normalize(criteria.ZipPrefix);
        var hasZip = zipPrefix != null;
        var zipPrefixLower = (zipPrefix ?? string.Empty).ToLowerInvariant();

        return a => !a.IsDeleted
            && (!hasCountry || a.Country.ToUpper() == countryCode)
            && (!hasCountryCodes || countryCodes.Contains(a.Country.ToUpper()))
            && (!hasState
                || anyCountryStateCodes.Contains(a.State.ToUpper())
                || stateCountryKeys.Contains(a.State.ToUpper() + StateCountryKeySeparator + a.Country.ToUpper())
                || (a.Country == string.Empty && countryBoundStateCodes.Contains(a.State.ToUpper())))
            && (!hasCity || a.City.ToUpper() == cityUpper)
            && (!hasZip || a.Zip.ToLower().StartsWith(zipPrefixLower));
    }

    public static Expression<Func<Client, bool>> AnyAddressOf(AddressLocationCriteria criteria)
        => AnyOf<Client>(c => c.Addresses, criteria);

    public static Expression<Func<TOwner, bool>> AnyOf<TOwner>(
        Expression<Func<TOwner, IEnumerable<Address>>> addresses, AddressLocationCriteria criteria)
    {
        var any = Expression.Call(
            typeof(Enumerable), nameof(Enumerable.Any), [typeof(Address)], addresses.Body, Matches(criteria));
        return Expression.Lambda<Func<TOwner, bool>>(any, addresses.Parameters);
    }

    private static string StateCountryKey(string stateCode, string countryCode)
        => stateCode.Trim().ToUpperInvariant() + StateCountryKeySeparator + countryCode.Trim().ToUpperInvariant();

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
