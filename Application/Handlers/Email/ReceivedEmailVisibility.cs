// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Group-visibility rule for received emails. An email has no client id; it belongs to the clients whose
/// private or office address equals its sender address. An email whose sender belongs to at least one client
/// outside the caller's group visibility is hidden and must be answered exactly like a missing email - even when
/// a visible client shares the address, because otherwise putting a hidden employee's address on a visible
/// client would expose that employee's mail. Emails whose sender matches no client stay unaffected.
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
        return visibleOwnerIds.Count < ownerIds.Count;
    }

    /// <summary>
    /// Removes every address that is also owned by a client hidden from the caller, so listing the mail of a
    /// visible client or group never returns mail sent from a hidden client's address.
    /// </summary>
    /// <param name="addresses">Lower-case sender addresses of visible clients</param>
    public static async Task<List<string>> ExcludeHiddenSenderAddressesAsync(
        IEmailQueryRepository emailQueryRepository,
        IClientVisibilityGuard clientVisibilityGuard,
        List<string> addresses,
        CancellationToken cancellationToken)
    {
        if (addresses.Count == 0)
        {
            return addresses;
        }

        var hiddenAddresses = await GetHiddenSenderAddressesAsync(emailQueryRepository, clientVisibilityGuard, cancellationToken);
        if (hiddenAddresses == null)
        {
            return addresses;
        }

        var hidden = hiddenAddresses.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return addresses.Where(address => !hidden.Contains(address)).ToList();
    }

    /// <summary>
    /// Lower-case sender addresses owned by at least one hidden client, or null when the caller sees every owner.
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
        var visibleClientIds = visible.Select(c => c.ClientId).ToHashSet();
        var hiddenAddresses = communications
            .Where(c => !visibleClientIds.Contains(c.ClientId))
            .Select(c => c.EmailAddress.ToLowerInvariant())
            .Distinct()
            .ToList();

        return hiddenAddresses.Count == 0 ? null : hiddenAddresses;
    }
}
