// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Existence plus group visibility of a referenced client. The visibility guard answers first (an unrestricted caller
/// without a query, a restricted one with the scoped client query that already excludes missing and soft-deleted rows);
/// the existence check then also catches unknown or deleted ids of unrestricted callers, which would otherwise end in a
/// foreign-key error or a silent reference to a deleted client.
/// </summary>
/// <param name="clientVisibilityGuard">Decides whether the calling user may see the client</param>
/// <param name="clientRepository">Answers whether the client exists; its query filter hides soft-deleted rows</param>

using Klacks.Api.Application.Interfaces;

namespace Klacks.Api.Application.Services.Clients;

public class ClientReferenceGuard : IClientReferenceGuard
{
    private readonly IClientVisibilityGuard _clientVisibilityGuard;
    private readonly IClientRepository _clientRepository;

    public ClientReferenceGuard(IClientVisibilityGuard clientVisibilityGuard, IClientRepository clientRepository)
    {
        _clientVisibilityGuard = clientVisibilityGuard;
        _clientRepository = clientRepository;
    }

    public Task<bool> ExistsVisibleAsync(Guid clientId, CancellationToken cancellationToken = default)
    {
        return ExistsVisibleAsync(_clientRepository, _clientVisibilityGuard, clientId, cancellationToken);
    }

    public async Task EnsureExistsVisibleAsync(Guid clientId, CancellationToken cancellationToken = default)
    {
        if (!await ExistsVisibleAsync(clientId, cancellationToken))
        {
            throw new KeyNotFoundException(string.Format(ClientVisibilityMessages.ClientNotFound, clientId));
        }
    }

    /// <summary>
    /// The rule itself, shared with the static skill helper ClientResolver so it exists only once.
    /// </summary>
    /// <param name="clientId">Client id taken from the request or the skill parameters</param>
    internal static async Task<bool> ExistsVisibleAsync(
        IClientRepository clientRepository,
        IClientVisibilityGuard clientVisibilityGuard,
        Guid clientId,
        CancellationToken cancellationToken)
    {
        return await clientVisibilityGuard.IsVisibleAsync(clientId, cancellationToken)
               && await clientRepository.Exists(clientId);
    }
}
