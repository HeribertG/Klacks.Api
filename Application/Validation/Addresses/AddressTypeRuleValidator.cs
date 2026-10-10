// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Shared check for the standalone address POST/PUT commands: the address type must be allowed for the
/// entity type of the owning client (ClientAddressTypeRules). On update an unchanged type of an already
/// stored address stays acceptable so legacy addresses can still be edited.
/// </summary>
/// <param name="clientRepository">Loads the entity type of the client that owns the address</param>
/// <param name="addressRepository">Loads the stored type of the address being updated</param>

using Klacks.Api.Application.DTOs.Staffs;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Validation.Clients;

namespace Klacks.Api.Application.Validation.Addresses;

public class AddressTypeRuleValidator
{
    private readonly IClientRepository _clientRepository;
    private readonly IAddressRepository _addressRepository;

    public AddressTypeRuleValidator(IClientRepository clientRepository, IAddressRepository addressRepository)
    {
        _clientRepository = clientRepository;
        _addressRepository = addressRepository;
    }

    public async Task<bool> IsAcceptableAsync(AddressResource address, bool isUpdate, CancellationToken cancellationToken)
    {
        var owner = await _clientRepository.GetTypeAndDisplayNameAsync(address.ClientId, cancellationToken);
        if (owner == null || ClientAddressTypeRules.IsAllowed(owner.Type, address.Type))
        {
            return true;
        }

        if (!isUpdate)
        {
            return false;
        }

        var stored = await _addressRepository.GetNoTracking(address.Id);
        return stored != null && stored.Type == address.Type;
    }
}
