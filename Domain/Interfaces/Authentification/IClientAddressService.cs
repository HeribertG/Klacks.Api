// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Interfaces.Authentification;
using Klacks.Api.Domain.Models.Staffs;

namespace Klacks.Api.Domain.Interfaces.Authentification;

/// <summary>
/// Service für die Verwaltung von Client-Adressen bei der LDAP-Synchronisierung.
/// </summary>
public interface IClientAddressService
{
    Task UpdateClientAddressAsync(Guid clientId, LdapUserEntry ldapUser);
    Task<Address> CreateNewClientAddressAsync(Guid clientId, LdapUserEntry ldapUser);
    Task<Address> CreateAddressFromOwnerSettingsAsync(Guid clientId);
    bool HasAddressData(LdapUserEntry ldapUser);
    bool AddressHasChanged(Address current, string street, string zip, string city, string state, string country);
}
