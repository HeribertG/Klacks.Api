// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The one privacy rule of the automatic address geocoding (owner decision 2026-10-24): addresses of customers
/// (main, work-site and invoicing addresses) are geocoded automatically; every address of an employee or an
/// external employee is a private address and is sent to the geocoder only when the installation allows it
/// (setting GEOCODING_EMPLOYEE_ADDRESSES_ENABLED, default off). The decision is made on the client type, never on
/// the address type: AddressTypeEnum.Employee means "main address" for customers too.
/// </summary>

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Services.Geocoding;

public static class AddressGeocodingPrivacy
{
    public static bool IsPrivateAddressOwner(EntityTypeEnum clientType) => clientType != EntityTypeEnum.Customer;

    public static bool MayGeocode(EntityTypeEnum clientType, bool employeeAddressesAllowed) =>
        employeeAddressesAllowed || !IsPrivateAddressOwner(clientType);
}
