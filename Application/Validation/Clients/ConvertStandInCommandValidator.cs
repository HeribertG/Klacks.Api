// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Checks the person data of a stand-in conversion with the rules of creating a person: the target type must be one a
/// stand-in may become (ClientTypeRules.ConversionTargetTypes), first name, name and a person gender are required
/// (ClientPersonRules) and the names fit their columns (ClientFieldLimits), and an address, when given, is a new one (no
/// id, so the geocoding check never takes it for a stored, unchanged address), must have a type allowed for the target type
/// (ClientAddressTypeRules), carry the date it is valid from (a required column) and pass the same geocoding check as on the client POST (AddressGeocodingValidator, which
/// also sets its coordinates). Whether the client is a stand-in is checked by the handler.
/// </summary>
/// <param name="geocodingService">Service for address geocoding</param>
/// <param name="stateResolver">Resolves state abbreviations from geocoding results</param>
/// <param name="countryResolver">Resolves the country name used for geocoding</param>
/// <param name="addressRepository">Loads the stored address to detect whether it changed</param>

using FluentValidation;
using Klacks.Api.Application.Commands.Clients;
using Klacks.Api.Application.DTOs.Staffs;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.RouteOptimization;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Services.Clients;
using Klacks.Api.Domain.Services.Common;

namespace Klacks.Api.Application.Validation.Clients;

public class ConvertStandInCommandValidator : AbstractValidator<ConvertStandInCommand>
{
    public const string TargetTypeMessage = "A stand-in can only be converted into an employee or an external employee.";
    public const string AddressValidFromMessage = "The address needs the date it is valid from.";
    public const string AddressIdMessage = "The address of a conversion is a new address and carries no id.";
    public const string NameTooLongMessage = "First name and name have at most {0} characters each.";

    public ConvertStandInCommandValidator(
        IGeocodingService geocodingService,
        StateAbbreviationResolver stateResolver,
        ICountryResolver countryResolver,
        IAddressRepository addressRepository)
    {
        RuleFor(x => x.Resource.TargetType)
            .Must(type => ClientTypeRules.CanConvert(EntityTypeEnum.StandIn, (EntityTypeEnum)type))
            .WithMessage(TargetTypeMessage);

        RuleFor(x => x.Resource.FirstName)
            .Must(ClientPersonRules.HasName)
            .WithMessage(ClientPersonRules.FirstNameRequiredMessage)
            .MaximumLength(ClientFieldLimits.NameMaxLength)
            .WithMessage(string.Format(NameTooLongMessage, ClientFieldLimits.NameMaxLength));

        RuleFor(x => x.Resource.Name)
            .Must(ClientPersonRules.HasName)
            .WithMessage(ClientPersonRules.NameRequiredMessage)
            .MaximumLength(ClientFieldLimits.NameMaxLength)
            .WithMessage(string.Format(NameTooLongMessage, ClientFieldLimits.NameMaxLength));

        RuleFor(x => x.Resource.Gender)
            .Must(ClientPersonRules.IsPersonGender)
            .WithMessage(ClientPersonRules.GenderRequiredMessage);

        When(x => x.Resource.Address != null, () =>
        {
            RuleFor(x => x.Resource.Address!.Id)
                .Equal(Guid.Empty)
                .WithMessage(AddressIdMessage);

            RuleFor(x => x.Resource.Address!.ValidFrom)
                .NotNull()
                .WithMessage(AddressValidFromMessage);

            RuleFor(x => x.Resource.Address!.Type)
                .Must((command, addressType) => ClientAddressTypeRules.IsAllowed((EntityTypeEnum)command.Resource.TargetType, addressType))
                .When(x => ClientTypeRules.CanConvert(EntityTypeEnum.StandIn, (EntityTypeEnum)x.Resource.TargetType))
                .WithMessage(ClientAddressTypeRules.NotAllowedMessage);

            RuleFor(x => (ICollection<AddressResource>)new List<AddressResource> { x.Resource.Address! })
                .SetValidator(new AddressGeocodingValidator(geocodingService, stateResolver, countryResolver, addressRepository))
                .OverridePropertyName("Resource.Address");
        });
    }
}
