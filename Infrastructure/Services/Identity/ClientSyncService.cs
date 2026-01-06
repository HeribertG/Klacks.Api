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
    private readonly IIdentityProviderRepository _providerRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ClientSyncService> _logger;

    public ClientSyncService(
        ILdapService ldapService,
        IClientRepository clientRepository,
        IMembershipRepository membershipRepository,
        IAddressRepository addressRepository,
        IIdentityProviderSyncLogRepository syncLogRepository,
        IIdentityProviderRepository providerRepository,
        IUnitOfWork unitOfWork,
        ILogger<ClientSyncService> logger)
    {
        _ldapService = ldapService;
        _clientRepository = clientRepository;
        _membershipRepository = membershipRepository;
        _addressRepository = addressRepository;
        _syncLogRepository = syncLogRepository;
        _providerRepository = providerRepository;
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
                    await CreateNewClient(provider, ldapUser);
                    result.NewClients++;
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
        var client = new Client
        {
            Id = Guid.NewGuid(),
            Name = ldapUser.Surname ?? ldapUser.DisplayName ?? "Unknown",
            FirstName = ldapUser.GivenName,
            Title = ldapUser.Title,
            Company = ldapUser.Company,
            Type = EntityTypeEnum.Employee,
            Gender = GenderEnum.Intersexuality
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

        if (HasAddressData(ldapUser))
        {
            var address = new Address
            {
                Id = Guid.NewGuid(),
                ClientId = client.Id,
                Type = AddressTypeEnum.Employee,
                Street = ldapUser.StreetAddress ?? string.Empty,
                Zip = ldapUser.PostalCode ?? string.Empty,
                City = ldapUser.City ?? string.Empty,
                State = ldapUser.State ?? string.Empty,
                Country = ldapUser.Country ?? string.Empty,
                ValidFrom = DateTime.UtcNow,
                AddressLine1 = string.Empty,
                AddressLine2 = string.Empty,
                Street2 = string.Empty,
                Street3 = string.Empty
            };

            await _addressRepository.Add(address);
        }

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

    private static bool HasAddressData(LdapUserEntry ldapUser)
    {
        return !string.IsNullOrEmpty(ldapUser.StreetAddress) ||
               !string.IsNullOrEmpty(ldapUser.PostalCode) ||
               !string.IsNullOrEmpty(ldapUser.City);
    }
}
