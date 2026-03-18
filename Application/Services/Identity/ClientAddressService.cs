// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Authentification;
using Klacks.Api.Domain.Models.Staffs;

namespace Klacks.Api.Application.Services.Identity;

/// <summary>
/// Service für die Verwaltung von Client-Adressen bei der LDAP-Synchronisierung.
/// </summary>
/// <param name="_addressRepository">Repository für Adress-CRUD-Operationen</param>
/// <param name="_settingsRepository">Repository zum Laden der Owner-Adress-Defaults</param>
/// <param name="_logger">Logger für Diagnose-Informationen</param>
public class ClientAddressService : IClientAddressService
{
    private readonly IAddressRepository _addressRepository;
    private readonly ISettingsRepository _settingsRepository;
    private readonly ILogger<ClientAddressService> _logger;

    public ClientAddressService(
        IAddressRepository addressRepository,
        ISettingsRepository settingsRepository,
        ILogger<ClientAddressService> logger)
    {
        _addressRepository = addressRepository;
        _settingsRepository = settingsRepository;
        _logger = logger;
    }

    public async Task UpdateClientAddressAsync(Guid clientId, LdapUserEntry ldapUser)
    {
        if (!HasAddressData(ldapUser))
        {
            return;
        }

        var (street, zip, city, state, country) = await ResolveAddressValuesAsync(ldapUser);

        var addresses = await _addressRepository.ClienList(clientId);
        var currentAddress = addresses
            .Where(a => a.Type == AddressTypeEnum.Employee)
            .OrderByDescending(a => a.ValidFrom)
            .FirstOrDefault();

        if (currentAddress != null && !AddressHasChanged(currentAddress, street, zip, city, state, country))
        {
            return;
        }

        var newAddress = CreateEmployeeAddress(clientId, street, zip, city, state, country);
        await _addressRepository.Add(newAddress);
    }

    public async Task<Address> CreateNewClientAddressAsync(Guid clientId, LdapUserEntry ldapUser)
    {
        var (street, zip, city, state, country) = await ResolveAddressValuesAsync(ldapUser);

        _logger.LogInformation("Creating address for client {ClientId}: Street={Street}, Zip={Zip}, City={City}, State={State}, Country={Country}",
            clientId, street, zip, city, state, country);
        _logger.LogInformation("LDAP values: Street={Street}, Zip={Zip}, City={City}, State={State}, Country={Country}",
            ldapUser.StreetAddress ?? "(null)", ldapUser.PostalCode ?? "(null)", ldapUser.City ?? "(null)",
            ldapUser.State ?? "(null)", ldapUser.Country ?? "(null)");

        var address = CreateEmployeeAddress(clientId, street, zip, city, state, country);
        await _addressRepository.Add(address);
        return address;
    }

    public async Task<Address> CreateAddressFromOwnerSettingsAsync(Guid clientId)
    {
        var streetSetting = await _settingsRepository.GetSetting("APP_ADDRESS_ADDRESS");
        var zipSetting = await _settingsRepository.GetSetting("APP_ADDRESS_ZIP");
        var placeSetting = await _settingsRepository.GetSetting("APP_ADDRESS_PLACE");
        var stateSetting = await _settingsRepository.GetSetting("APP_ADDRESS_STATE");
        var countrySetting = await _settingsRepository.GetSetting("APP_ADDRESS_COUNTRY");

        return new Address
        {
            Id = Guid.NewGuid(),
            ClientId = clientId,
            Type = AddressTypeEnum.Employee,
            Street = streetSetting?.Value ?? string.Empty,
            Zip = zipSetting?.Value ?? string.Empty,
            City = placeSetting?.Value ?? string.Empty,
            State = stateSetting?.Value ?? string.Empty,
            Country = countrySetting?.Value ?? "CH",
            ValidFrom = DateTime.UtcNow,
            AddressLine1 = string.Empty,
            AddressLine2 = string.Empty,
            Street2 = string.Empty,
            Street3 = string.Empty
        };
    }

    public bool HasAddressData(LdapUserEntry ldapUser)
    {
        return !string.IsNullOrEmpty(ldapUser.StreetAddress) ||
               !string.IsNullOrEmpty(ldapUser.PostalCode) ||
               !string.IsNullOrEmpty(ldapUser.City);
    }

    public bool AddressHasChanged(Address current, string street, string zip, string city, string state, string country)
    {
        return current.Street != street ||
               current.Zip != zip ||
               current.City != city ||
               current.State != state ||
               current.Country != country;
    }

    private async Task<(string street, string zip, string city, string state, string country)> ResolveAddressValuesAsync(LdapUserEntry ldapUser)
    {
        var streetSetting = await _settingsRepository.GetSetting("APP_ADDRESS_ADDRESS");
        var zipSetting = await _settingsRepository.GetSetting("APP_ADDRESS_ZIP");
        var placeSetting = await _settingsRepository.GetSetting("APP_ADDRESS_PLACE");
        var stateSetting = await _settingsRepository.GetSetting("APP_ADDRESS_STATE");
        var countrySetting = await _settingsRepository.GetSetting("APP_ADDRESS_COUNTRY");

        var street = ldapUser.StreetAddress ?? streetSetting?.Value ?? "Unbekannt";
        var zip = ldapUser.PostalCode ?? zipSetting?.Value ?? "0000";
        var city = ldapUser.City ?? placeSetting?.Value ?? "Unbekannt";
        var state = ldapUser.State ?? stateSetting?.Value ?? "ZH";
        var country = ldapUser.Country ?? countrySetting?.Value ?? "CH";

        return (street, zip, city, state, country);
    }

    private static Address CreateEmployeeAddress(Guid clientId, string street, string zip, string city, string state, string country)
    {
        return new Address
        {
            ClientId = clientId,
            Type = AddressTypeEnum.Employee,
            Street = street,
            Zip = zip,
            City = city,
            State = state,
            Country = country,
            ValidFrom = DateTime.UtcNow,
            AddressLine1 = string.Empty,
            AddressLine2 = string.Empty,
            Street2 = string.Empty,
            Street3 = string.Empty
        };
    }
}
