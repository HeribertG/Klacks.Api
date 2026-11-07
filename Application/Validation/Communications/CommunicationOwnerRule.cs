// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Shared check for the standalone communication POST/PUT commands: a stand-in carries no communication, so an entry
/// for a stand-in is refused. An unknown owner is left to the handler, which answers it like a hidden client.
/// </summary>
/// <param name="clientRepository">Loads the entity type of the client that owns the communication</param>

using Klacks.Api.Application.DTOs.Settings;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Services.Clients;

namespace Klacks.Api.Application.Validation.Communications;

public class CommunicationOwnerRule
{
    private readonly IClientRepository _clientRepository;

    public CommunicationOwnerRule(IClientRepository clientRepository)
    {
        _clientRepository = clientRepository;
    }

    public async Task<bool> IsAcceptableAsync(CommunicationResource communication, CancellationToken cancellationToken)
    {
        var owner = await _clientRepository.GetTypeAndDisplayNameAsync(communication.ClientId, cancellationToken);
        return owner == null || !ClientTypeRules.IsStandIn(owner.Type);
    }
}
