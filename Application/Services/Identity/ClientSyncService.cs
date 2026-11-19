// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using System.Diagnostics.CodeAnalysis;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Authentification;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Application.DTOs.IdentityProviders;
using Klacks.Api.Domain.DTOs.IdentityProviders;
using Klacks.Api.Domain.Interfaces.Authentification;

namespace Klacks.Api.Application.Services.Identity;

/// <summary>
/// Imports directory accounts as clients of one provider: creates, updates, links by LdapExternalId and ends the
/// membership of clients whose entry the directory no longer returns. Keys every entry on LdapUserEntry.StableId and
/// re-keys a client still stored under its distinguished name. Only enabled entries are imported: a disabled entry
/// counts as missing, so its client is deactivated and none is created for it. An incomplete result, or one without any
/// enabled entry, aborts the sync before anything is written, so a size limit can never end memberships of people who
/// are still employed.
/// </summary>
/// <param name="ldapService">Reads the directory</param>
/// <param name="clientRepository">Reads and writes the clients</param>
/// <param name="membershipRepository">Writes memberships of new clients</param>
/// <param name="clientAddressService">Writes the client addresses</param>
/// <param name="syncLogRepository">Reads and writes the sync log per directory entry</param>
/// <param name="unitOfWork">Commits the sync</param>
/// <param name="logger">Records the outcome</param>
public class ClientSyncService : IClientSyncService
{
    public const string IncompleteResultMessage =
        "The directory returned an incomplete user list (size or time limit). The import was aborted and nothing was changed.";
    public const string EmptyResultMessage =
        "The directory returned no enabled users. The import was aborted and nothing was changed.";
    public const string SyncFailedMessage = "The import failed. See the server log for details.";

    private readonly ILdapService _ldapService;
    private readonly IClientRepository _clientRepository;
    private readonly IMembershipRepository _membershipRepository;
    private readonly IClientAddressService _clientAddressService;
    private readonly IIdentityProviderSyncLogRepository _syncLogRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ClientSyncService> _logger;

    public ClientSyncService(
        ILdapService ldapService,
        IClientRepository clientRepository,
        IMembershipRepository membershipRepository,
        IClientAddressService clientAddressService,
        IIdentityProviderSyncLogRepository syncLogRepository,
        IUnitOfWork unitOfWork,
        ILogger<ClientSyncService> logger)
    {
        _ldapService = ldapService;
        _clientRepository = clientRepository;
        _membershipRepository = membershipRepository;
        _clientAddressService = clientAddressService;
        _syncLogRepository = syncLogRepository;
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

            var searchResult = await _ldapService.GetUsersAsync(provider);
            _logger.LogInformation("Retrieved {Count} users from LDAP (complete: {IsComplete})", searchResult.Entries.Count, searchResult.IsComplete);

            var enabledEntries = searchResult.Entries.Where(entry => entry.IsEnabled).ToList();

            if (!searchResult.IsComplete || enabledEntries.Count == 0)
            {
                var abortMessage = searchResult.IsComplete ? EmptyResultMessage : IncompleteResultMessage;
                _logger.LogWarning("Client sync aborted for provider {ProviderId}: {Reason}", provider.Id, abortMessage);
                result.Success = false;
                result.ErrorMessage = abortMessage;
                provider.LastSyncError = abortMessage;
                await _unitOfWork.CompleteAsync();
                return result;
            }

            var existingSyncLogs = await _syncLogRepository.GetByProviderId(provider.Id);
            var existingByExternalId = existingSyncLogs.ToDictionary(s => s.ExternalId);

            var processedExternalIds = new HashSet<string>();

            foreach (var ldapUser in enabledEntries)
            {
                var externalId = ldapUser.StableId;
                processedExternalIds.Add(externalId);

                if (existingByExternalId.TryGetValue(externalId, out var existingLog)
                    || TryRekeyFromDistinguishedName(ldapUser, existingByExternalId, out existingLog))
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
            result.ErrorMessage = SyncFailedMessage;

            provider.LastSyncError = SyncFailedMessage;
            await _unitOfWork.CompleteAsync();
        }

        return result;
    }

    private async Task CreateNewClient(IdentityProvider provider, LdapUserEntry ldapUser)
    {
        var externalId = ldapUser.StableId;

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
            ValidUntil = null
        };

        await _membershipRepository.Add(membership);

        await _clientAddressService.CreateNewClientAddressAsync(client.Id, ldapUser);

        var syncLog = new IdentityProviderSyncLog
        {
            Id = Guid.NewGuid(),
            IdentityProviderId = provider.Id,
            ClientId = client.Id,
            ExternalId = externalId,
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
        client.LdapExternalId = ldapUser.StableId;

        await _clientAddressService.UpdateClientAddressAsync(client.Id, ldapUser);

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

        // _clientRepository.Get() returns a detached (AsNoTracking) entity - mutations above are only
        // persisted once explicitly written back via Put().
        await _clientRepository.Put(client);

        _logger.LogDebug("Updated client {Name} from LDAP user {DN}", client.Name, ldapUser.DistinguishedName);
    }

    /// <summary>
    /// Finds the sync log of an entry that an earlier import stored under its distinguished name (before entryUUID was
    /// read) and moves it to the stable id, so the client is updated instead of duplicated.
    /// </summary>
    private static bool TryRekeyFromDistinguishedName(
        LdapUserEntry ldapUser,
        Dictionary<string, IdentityProviderSyncLog> existingByExternalId,
        [NotNullWhen(true)] out IdentityProviderSyncLog? syncLog)
    {
        var stableId = ldapUser.StableId;
        if (string.Equals(stableId, ldapUser.DistinguishedName, StringComparison.Ordinal)
            || existingByExternalId.ContainsKey(stableId)
            || !existingByExternalId.Remove(ldapUser.DistinguishedName, out var found))
        {
            syncLog = null;
            return false;
        }

        found.ExternalId = stableId;
        existingByExternalId[stableId] = found;
        syncLog = found;
        return true;
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

            // _clientRepository.Get() returns a detached (AsNoTracking) entity - the membership change
            // above is only persisted once explicitly written back via Put().
            await _clientRepository.Put(client);
        }

        syncLog.IsActiveInSource = false;
        syncLog.LastSyncTime = DateTime.UtcNow;
    }

    private async Task LinkExistingClientToProvider(IdentityProvider provider, LdapUserEntry ldapUser, Guid clientId)
    {
        var externalId = ldapUser.StableId;

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
}
