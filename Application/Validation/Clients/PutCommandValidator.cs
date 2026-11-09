// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using FluentValidation;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.DTOs.Staffs;
using Klacks.Api.Domain.Interfaces.RouteOptimization;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Services.Common;

namespace Klacks.Api.Application.Validation.Clients;

/// <summary>
/// Validates client updates: addresses, client type, legal entity, contracts, and that closing a group membership leaves
/// no planned works after its end. The membership check runs only for a client and a group the caller can see, so a hidden
/// client or group is answered by the handler like a missing one instead of a validation error that reveals planned works.
/// </summary>
/// <param name="clientVisibilityGuard">Decides whether the caller can see the client being updated</param>
/// <param name="groupVisibilityGuard">Decides whether the caller can see the group of a membership</param>
public class PutCommandValidator : AbstractValidator<PutCommand<ClientResource>>
{
    public PutCommandValidator(
        IGeocodingService geocodingService,
        StateAbbreviationResolver stateResolver,
        ICountryResolver countryResolver,
        IAddressRepository addressRepository,
        IShiftRepository shiftRepository,
        IClientRepository clientRepository,
        IClientVisibilityGuard clientVisibilityGuard,
        IGroupVisibilityGuard groupVisibilityGuard)
    {
        When(x => !x.Resource.SkipAddressValidation, () =>
        {
            RuleFor(x => x.Resource.Addresses)
                .SetValidator(new AddressGeocodingValidator(geocodingService, stateResolver, countryResolver, addressRepository));
        });

        RuleFor(x => x.Resource)
            .SetValidator(new ClientAddressTypeValidator(clientRepository, addressRepository));

        RuleFor(x => x.Resource)
            .SetValidator(new ClientTypeValidator());

        When(x => x.Resource.LegalEntity, () =>
        {
            RuleFor(x => x.Resource.Company)
                .Must(ClientLegalEntityRules.HasCompany)
                .WithMessage("address.edit-address.address-persona.validation.company-required");

            RuleFor(x => x.Resource.Type)
                .Equal((int)ClientLegalEntityRules.RequiredType)
                .WithMessage("address.edit-address.address-persona.validation.legal-entity-must-be-customer");

            RuleFor(x => x.Resource.Addresses)
                .Must(addresses => addresses != null
                    && addresses.Any(a => ClientLegalEntityRules.IsCompleteAddress(a.Zip, a.City, a.Country)))
                .WithMessage("address.edit-address.address-persona.validation.address-required");
        });

        When(x => !x.Resource.LegalEntity, () =>
        {
            RuleFor(x => x.Resource.FirstName)
                .Must(ClientPersonRules.HasName)
                .When(x => ClientPersonRules.RequiresFirstNameAndGender(x.Resource.Type))
                .WithMessage(ClientPersonRules.FirstNameRequiredMessage);

            RuleFor(x => x.Resource.Name)
                .Must(ClientPersonRules.HasName)
                .WithMessage(ClientPersonRules.NameRequiredMessage);

            RuleFor(x => x.Resource.Gender)
                .Must(ClientPersonRules.IsPersonGender)
                .When(x => ClientPersonRules.RequiresFirstNameAndGender(x.Resource.Type))
                .WithMessage(ClientPersonRules.GenderRequiredMessage);
        });

        RuleFor(x => x.Resource.ClientContracts)
            .Must(contracts =>
            {
                if (contracts == null || !contracts.Any())
                {
                    return true;
                }

                foreach (var contract in contracts)
                {
                    if (contract.UntilDate.HasValue && contract.FromDate > contract.UntilDate.Value)
                    {
                        return false;
                    }
                }
                return true;
            })
            .WithMessage("address.edit-address.contracts.validation.all-dates-valid");

        RuleFor(x => x.Resource.ClientContracts)
            .Must(contracts =>
            {
                if (contracts == null || !contracts.Any())
                {
                    return true;
                }
                return contracts.Any(c => c.IsActive);
            })
            .WithMessage("address.edit-address.contracts.validation.at-least-one-active");

        RuleFor(x => x)
            .MustAsync(async (command, ct) =>
            {
                if (command.Resource.GroupItems == null
                    || !await clientVisibilityGuard.IsVisibleAsync(command.Resource.Id, ct))
                {
                    return true;
                }

                foreach (var item in command.Resource.GroupItems.Where(gi => gi.ValidUntil.HasValue))
                {
                    if (!await groupVisibilityGuard.IsGroupVisibleAsync(item.GroupId, ct))
                    {
                        continue;
                    }

                    var afterDate = DateOnly.FromDateTime(item.ValidUntil!.Value);
                    if (await shiftRepository.HasWorksForClientInGroupAsync(command.Resource.Id, item.GroupId, afterDate, ct))
                    {
                        return false;
                    }
                }
                return true;
            })
            .WithMessage("group.validation.membership-close-has-future-works");
    }
}
