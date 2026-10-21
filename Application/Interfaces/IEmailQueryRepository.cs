// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Repository for complex email query operations across multiple entities.
/// @param folder - The email folder to filter by (e.g., ClientAssignedFolder)
/// @param communicationTypes - The communication types to include (e.g., PrivateMail, OfficeMail)
/// </summary>

namespace Klacks.Api.Application.Interfaces;

public interface IEmailQueryRepository
{
    Task<List<string>> GetDistinctAssignedEmailAddressesAsync(string folder, CancellationToken cancellationToken = default);

    Task<List<ClientEmailInfo>> GetClientsWithEmailCommunicationsAsync(CancellationToken cancellationToken = default);

    Task<int> CountEmailsByAddressAsync(string folder, string emailAddress, CancellationToken cancellationToken = default);

    Task<int> CountUnreadEmailsByAddressAsync(string folder, string emailAddress, CancellationToken cancellationToken = default);

    Task<List<string>> GetEmailAddressesByClientAsync(Guid clientId, CancellationToken cancellationToken = default);

    Task<List<Guid>> GetClientIdsByGroupIdsAsync(HashSet<Guid> groupIds, CancellationToken cancellationToken = default);

    Task<List<string>> GetEmailAddressesByClientIdsAsync(List<Guid> clientIds, CancellationToken cancellationToken = default);

    Task<List<Guid>> GetClientIdsByEmailAddressAsync(string emailAddress, CancellationToken cancellationToken = default);

    Task<ReceivedEmailQueryResult> GetEmailsByAddressesAsync(
        string folder,
        List<string> emailAddresses,
        int skip,
        int take,
        CancellationToken cancellationToken = default);
}
