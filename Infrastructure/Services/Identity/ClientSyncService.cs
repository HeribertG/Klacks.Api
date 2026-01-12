using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Authentification;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Presentation.DTOs.IdentityProviders;

namespace Klacks.Api.Infrastructure.Services.Identity;

public class ClientSyncService : IClientSyncService
{
    private readonly ILdapService _ldapService;
    private readonly IClientRepository _clientRepository;
    private readonly IMembershipRepository _membershipRepository;
    private readonly IAddressRepository _addressRepository;
    private readonly IIdentityProviderSyncLogRepository _syncLogRepository;
    private readonly ISettingsRepository _settingsRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ClientSyncService> _logger;

    public ClientSyncService(
        ILdapService ldapService,
        IClientRepository clientRepository,
        IMembershipRepository membershipRepository,
        IAddressRepository addressRepository,
        IIdentityProviderSyncLogRepository syncLogRepository,
        ISettingsRepository settingsRepository,
        IUnitOfWork unitOfWork,
        ILogger<ClientSyncService> logger)
    {
        _ldapService = ldapService;
        _clientRepository = clientRepository;
        _membershipRepository = membershipRepository;
        _addressRepository = addressRepository;
        _syncLogRepository = syncLogRepository;
        _settingsRepository = settingsRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<IdentityProviderSyncResultResource> SyncClientsAsync(IdentityProvider provider)
    {
        var result = new IdentityProviderSyncResultResource
        {
            SyncTime = DateTime.UtcNow
        };

        try
        {
            _logger.LogInformation("Starting client sync for provider {Name}", provider.Name);

            var ldapUsers = await _ldapService.GetUsersAsync(provider);
            _logger.LogInformation("Retrieved {Count} users from LDAP", ldapUsers.Count);

            var existingSyncLogs = await _syncLogRepository.GetByProviderId(provider.Id);
            var existingByExternalId = existingSyncLogs.ToDictionary(s => s.ExternalId);

            var processedExternalIds = new HashSet<string>();

            foreach (var ldapUser in ldapUsers)
            {
                var externalId = ldapUser.ObjectGuid ?? ldapUser.DistinguishedName;
                processedExternalIds.Add(externalId);

                if (existingByExternalId.TryGetValue(externalId, out var existingLog))
                {
                    await UpdateExistingClient(provider, ldapUser, existingLog);
                    result.UpdatedClients++;
                }
                else
                {
                    var existingClient = await _clientRepository.GetByLdapExternalIdAsync(externalId);
                    if (existingClient != null)
                    {
                        await LinkExistingClientToProvider(provider, ldapUser, existingClient.Id);
                        result.UpdatedClients++;
                        _logger.LogInformation("Linked existing client {ClientId} to provider {Provider} (found by LdapExternalId)", existingClient.Id, provider.Name);
                    }
                    else
                    {
                        await CreateNewClient(provider, ldapUser);
                        result.NewClients++;
                    }
                }

                result.TotalProcessed++;
            }

            foreach (var syncLog in existingSyncLogs)
            {
                if (!processedExternalIds.Contains(syncLog.ExternalId) && syncLog.IsActiveInSource)
                {
                    await DeactivateClient(syncLog);
                    result.DeactivatedClients++;
                }
            }

            provider.LastSyncTime = DateTime.UtcNow;
            provider.LastSyncCount = result.TotalProcessed;
            provider.LastSyncError = null;

            await _unitOfWork.CompleteAsync();

            result.Success = true;
            _logger.LogInformation(
                "Client sync completed: {Total} processed, {New} new, {Updated} updated, {Deactivated} deactivated",
                result.TotalProcessed, result.NewClients, result.UpdatedClients, result.DeactivatedClients);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Client sync failed for provider {Name}", provider.Name);
            result.Success = false;
            result.ErrorMessage = ex.Message;

            provider.LastSyncError = ex.Message;
            await _unitOfWork.CompleteAsync();
        }

        return result;
    }

    private async Task CreateNewClient(IdentityProvider provider, LdapUserEntry ldapUser)
    {
        var externalId = ldapUser.ObjectGuid ?? ldapUser.DistinguishedName;

        var client = new Client
        {
            Id = Guid.NewGuid(),
            Name = ldapUser.Surname ?? ldapUser.DisplayName ?? "Unknown",
            FirstName = ldapUser.GivenName,
            Title = ldapUser.Title,
            Company = ldapUser.Company,
            Type = EntityTypeEnum.Employee,
            Gender = DetermineGenderFromPersonalTitle(ldapUser.PersonalTitle),
            IdentityProviderId = provider.Id,
            LdapExternalId = externalId
        };

        await _clientRepository.Add(client);

        var membership = new Membership
        {
            Id = Guid.NewGuid(),
            ClientId = client.Id,
            ValidFrom = ldapUser.WhenCreated ?? DateTime.UtcNow,
            ValidUntil = null,
            Type = 0
        };

        await _membershipRepository.Add(membership);

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

        _logger.LogInformation("Creating address for {Name}: Street={Street}, Zip={Zip}, City={City}, State={State}, Country={Country}",
            client.Name, street, zip, city, state, country);
        _logger.LogInformation("LDAP values: Street={Street}, Zip={Zip}, City={City}, State={State}, Country={Country}",
            ldapUser.StreetAddress ?? "(null)", ldapUser.PostalCode ?? "(null)", ldapUser.City ?? "(null)",
            ldapUser.State ?? "(null)", ldapUser.Country ?? "(null)");

        var address = new Address
        {
            ClientId = client.Id,
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

        await _addressRepository.Add(address);

        var syncLog = new IdentityProviderSyncLog
        {
            Id = Guid.NewGuid(),
            IdentityProviderId = provider.Id,
            ClientId = client.Id,
            ExternalId = ldapUser.ObjectGuid ?? ldapUser.DistinguishedName,
            ExternalDn = ldapUser.DistinguishedName,
            LastSyncTime = DateTime.UtcNow,
            IsActiveInSource = ldapUser.IsEnabled
        };

        await _syncLogRepository.Add(syncLog);

        _logger.LogDebug("Created new client {Name} from LDAP user {DN}", client.Name, ldapUser.DistinguishedName);
    }

    private async Task UpdateExistingClient(IdentityProvider provider, LdapUserEntry ldapUser, IdentityProviderSyncLog syncLog)
    {
        var client = await _clientRepository.Get(syncLog.ClientId);
        if (client == null)
        {
            _logger.LogWarning("Client {ClientId} not found for sync log update", syncLog.ClientId);
            return;
        }

        client.Name = ldapUser.Surname ?? ldapUser.DisplayName ?? client.Name;
        client.FirstName = ldapUser.GivenName ?? client.FirstName;
        client.Title = ldapUser.Title ?? client.Title;
        client.Company = ldapUser.Company ?? client.Company;

        await UpdateClientAddress(client.Id, ldapUser);

        var wasInactive = !syncLog.IsActiveInSource;
        var isNowActive = ldapUser.IsEnabled;

        if (wasInactive && isNowActive)
        {
            var membership = client.Membership;
            if (membership != null && membership.ValidUntil.HasValue)
            {
                membership.ValidFrom = DateTime.UtcNow;
                membership.ValidUntil = null;
                _logger.LogInformation("Reactivated client {Name}", client.Name);
            }
        }

        syncLog.LastSyncTime = DateTime.UtcNow;
        syncLog.IsActiveInSource = ldapUser.IsEnabled;
        syncLog.ExternalDn = ldapUser.DistinguishedName;

        _logger.LogDebug("Updated client {Name} from LDAP user {DN}", client.Name, ldapUser.DistinguishedName);
    }

    private async Task UpdateClientAddress(Guid clientId, LdapUserEntry ldapUser)
    {
        if (!HasAddressData(ldapUser))
        {
            return;
        }

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

        var addresses = await _addressRepository.ClienList(clientId);
        var currentAddress = addresses
            .Where(a => a.Type == AddressTypeEnum.Employee)
            .OrderByDescending(a => a.ValidFrom)
            .FirstOrDefault();

        if (currentAddress != null && !AddressHasChanged(currentAddress, street, zip, city, state, country))
        {
            return;
        }

        var newAddress = new Address
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
        await _addressRepository.Add(newAddress);
    }

    private static bool AddressHasChanged(Address current, string street, string zip, string city, string state, string country)
    {
        return current.Street != street ||
               current.Zip != zip ||
               current.City != city ||
               current.State != state ||
               current.Country != country;
    }

    private async Task DeactivateClient(IdentityProviderSyncLog syncLog)
    {
        var client = await _clientRepository.Get(syncLog.ClientId);
        if (client == null)
        {
            _logger.LogWarning("Client {ClientId} not found for deactivation", syncLog.ClientId);
            return;
        }

        var membership = client.Membership;
        if (membership != null && !membership.ValidUntil.HasValue)
        {
            membership.ValidUntil = DateTime.UtcNow;
            _logger.LogInformation("Deactivated client {Name} - no longer in LDAP", client.Name);
        }

        syncLog.IsActiveInSource = false;
        syncLog.LastSyncTime = DateTime.UtcNow;
    }

    private async Task LinkExistingClientToProvider(IdentityProvider provider, LdapUserEntry ldapUser, Guid clientId)
    {
        var externalId = ldapUser.ObjectGuid ?? ldapUser.DistinguishedName;

        var syncLog = new IdentityProviderSyncLog
        {
            Id = Guid.NewGuid(),
            IdentityProviderId = provider.Id,
            ClientId = clientId,
            ExternalId = externalId,
            ExternalDn = ldapUser.DistinguishedName,
            LastSyncTime = DateTime.UtcNow,
            IsActiveInSource = ldapUser.IsEnabled
        };

        await _syncLogRepository.Add(syncLog);
    }

    private static bool HasAddressData(LdapUserEntry ldapUser)
    {
        return !string.IsNullOrEmpty(ldapUser.StreetAddress) ||
               !string.IsNullOrEmpty(ldapUser.PostalCode) ||
               !string.IsNullOrEmpty(ldapUser.City);
    }

    private static GenderEnum DetermineGenderFromPersonalTitle(string? personalTitle)
    {
        if (string.IsNullOrEmpty(personalTitle))
        {
            return GenderEnum.Intersexuality;
        }

        var title = personalTitle.ToLowerInvariant().Trim();

        if (title is "herr" or "mr" or "mr." or "monsieur" or "m." or "signor")
        {
            return GenderEnum.Male;
        }

        if (title is "frau" or "mrs" or "mrs." or "ms" or "ms." or "madame" or "mme" or "mme." or "signora")
        {
            return GenderEnum.Female;
        }

        return GenderEnum.Intersexuality;
    }

    private async Task<Address> CreateAddressFromOwnerSettings(Guid clientId)
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
}
