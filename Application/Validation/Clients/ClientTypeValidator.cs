// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The type rules of a client shared by the client POST/PUT validators: the type must be a defined EntityTypeEnum, and a
/// stand-in carries no communication and no contract (its addresses are refused by ClientAddressTypeRules, its person
/// fields by ClientPersonRules).
/// </summary>

using FluentValidation;
using Klacks.Api.Application.DTOs.Staffs;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Services.Clients;

namespace Klacks.Api.Application.Validation.Clients;

public class ClientTypeValidator : AbstractValidator<ClientResource>
{
    public const string InvalidTypeMessage = "The client type is not valid.";
    public const string StandInCommunicationsMessage = "A stand-in has no communications.";
    public const string StandInContractsMessage = "A stand-in has no contracts.";

    public ClientTypeValidator()
    {
        RuleFor(client => client.Type)
            .Must(type => Enum.IsDefined((EntityTypeEnum)type))
            .WithMessage(InvalidTypeMessage);

        When(client => ClientTypeRules.IsStandIn((EntityTypeEnum)client.Type), () =>
        {
            RuleFor(client => client.Communications)
                .Must(communications => communications == null || communications.Count == 0)
                .WithMessage(StandInCommunicationsMessage);

            RuleFor(client => client.ClientContracts)
                .Must(contracts => contracts == null || contracts.Count == 0)
                .WithMessage(StandInContractsMessage);
        });
    }
}
