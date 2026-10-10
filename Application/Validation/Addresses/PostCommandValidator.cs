// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Validates a standalone address creation: the address type must be allowed for the owning client's type.
/// </summary>
/// <param name="clientRepository">Loads the entity type of the owning client</param>
/// <param name="addressRepository">Loads stored addresses (used by the shared type rule)</param>

using FluentValidation;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.DTOs.Staffs;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Validation.Clients;

namespace Klacks.Api.Application.Validation.Addresses;

public class PostCommandValidator : AbstractValidator<PostCommand<AddressResource>>
{
    public PostCommandValidator(IClientRepository clientRepository, IAddressRepository addressRepository)
    {
        var typeRule = new AddressTypeRuleValidator(clientRepository, addressRepository);

        RuleFor(x => x.Resource)
            .MustAsync((address, ct) => typeRule.IsAcceptableAsync(address, false, ct))
            .WithMessage(ClientAddressTypeRules.NotAllowedMessage);
    }
}
