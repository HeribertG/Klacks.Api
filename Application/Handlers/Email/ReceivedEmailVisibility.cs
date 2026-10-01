// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Group-visibility rule for received emails. An email has no client id; it belongs to the clients whose
/// private or office address equals its sender address. An email whose sender belongs only to clients
/// outside the caller's group visibility is hidden and must be answered exactly like a missing email.
/// Emails whose sender matches no client stay unaffected.
/// </summary>
/// <param name="emailQueryRepository">Resolves the clients that own a sender address</param>
/// <param name="clientVisibilityGuard">Decides which of those clients the calling user may see</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Email;

namespace Klacks.Api.Application.Handlers.Email;

internal static class ReceivedEmailVisibility
{
    public static async Task<bool> IsHiddenAsync(
        IEmailQueryRepository emailQueryRepository,
        IClientVisibilityGuard clientVisibilityGuard,
        ReceivedEmail email,
        CancellationToken cancellationToken)
    {
        var ownerIds = await emailQueryRepository.GetClientIdsByEmailAddressAsync(email.FromAddress, cancellationToken);
        if (ownerIds.Count == 0)
        {
            return false;
        }

        var visibleOwnerIds = await clientVisibilityGuard.FilterVisibleAsync(ownerIds, id => id, cancellationToken);
        return visibleOwnerIds.Count == 0;
    }

    /// <summary>
    /// Lower-case sender addresses owned only by hidden clients, or null when the caller sees every owner.
    /// </summary>
    public static async Task<List<string>?> GetHiddenSenderAddressesAsync(
        IEmailQueryRepository emailQueryRepository,
        IClientVisibilityGuard clientVisibilityGuard,
        CancellationToken cancellationToken)
    {
        var communications = await emailQueryRepository.GetClientsWithEmailCommunicationsAsync(cancellationToken);
        if (communications.Count == 0)
        {
            return null;
        }

        var visible = await clientVisibilityGuard.FilterVisibleAsync(communications, c => c.ClientId, cancellationToken);
        var visibleAddresses = visible
            .Select(c => c.EmailAddress.ToLowerInvariant())
            .ToHashSet();
        var hiddenAddresses = communications
            .Select(c => c.EmailAddress.ToLowerInvariant())
            .Where(address => !visibleAddresses.Contains(address))
            .Distinct()
            .ToList();

        return hiddenAddresses.Count == 0 ? null : hiddenAddresses;
    }
}
