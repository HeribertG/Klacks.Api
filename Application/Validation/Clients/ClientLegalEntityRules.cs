// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The rules a legal entity (a company without a person name) must satisfy, shared by the client POST/PUT
/// validators and the employee import so both accept exactly the same records: a company name, the client
/// type Customer and at least one address with postcode, city and country.
/// </summary>
/// <param name="zip">Postcode of an address</param>
/// <param name="city">City of an address</param>
/// <param name="country">Country code of an address</param>

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.Validation.Clients;

public static class ClientLegalEntityRules
{
    public const EntityTypeEnum RequiredType = EntityTypeEnum.Customer;

    public static bool HasCompany(string? company) => !string.IsNullOrWhiteSpace(company);

    public static bool IsCompleteAddress(string? zip, string? city, string? country) =>
        !string.IsNullOrWhiteSpace(zip) && !string.IsNullOrWhiteSpace(city) && !string.IsNullOrWhiteSpace(country);
}
