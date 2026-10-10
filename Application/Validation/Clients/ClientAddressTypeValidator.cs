// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Rejects client addresses whose type is not allowed for the client's entity type. An address that is
/// already stored with the same type stays acceptable while the client's entity type is unchanged, so
/// legacy addresses never block unrelated edits; new or retyped addresses, and every address after an
/// entity type change, must satisfy ClientAddressTypeRules.
/// </summary>
/// <param name="clientRepository">Loads the stored entity type of the client being updated</param>
/// <param name="addressRepository">Loads the stored type of an existing address</param>

using FluentValidation;
using Klacks.Api.Application.DTOs.Staffs;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.Validation.Clients;

public class ClientAddressTypeValidator : AbstractValidator<ClientResource>
{
    private readonly IClientRepository _clientRepository;
    private readonly IAddressRepository _addressRepository;

    public ClientAddressTypeValidator(IClientRepository clientRepository, IAddressRepository addressRepository)
    {
        _clientRepository = clientRepository;
        _addressRepository = addressRepository;

        RuleFor(client => client)
            .MustAsync(HasOnlyAcceptableAddressTypesAsync)
            .When(client => client.Addresses != null && client.Addresses.Any())
            .WithMessage(ClientAddressTypeRules.NotAllowedMessage);
    }

    private async Task<bool> HasOnlyAcceptableAddressTypesAsync(ClientResource client, CancellationToken cancellationToken)
    {
        var clientType = (EntityTypeEnum)client.Type;
        var violations = client.Addresses.Where(a => !ClientAddressTypeRules.IsAllowed(clientType, a.Type)).ToList();
        if (violations.Count == 0)
        {
            return true;
        }

        var stored = client.Id == Guid.Empty
            ? null
            : await _clientRepository.GetTypeAndDisplayNameAsync(client.Id, cancellationToken);
        if (stored == null || stored.Type != clientType)
        {
            return false;
        }

        foreach (var address in violations)
        {
            if (!await IsStoredWithSameTypeAsync(address))
            {
                return false;
            }
        }

        return true;
    }

    private async Task<bool> IsStoredWithSameTypeAsync(AddressResource address)
    {
        if (address.Id == Guid.Empty)
        {
            return false;
        }

        var stored = await _addressRepository.GetNoTracking(address.Id);
        return stored != null && stored.Type == address.Type;
    }
}
