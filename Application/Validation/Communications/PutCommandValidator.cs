// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Validates a standalone communication update: the owning client must not be a stand-in.
/// </summary>
/// <param name="clientRepository">Loads the entity type of the owning client</param>

using FluentValidation;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.DTOs.Settings;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Validation.Clients;

namespace Klacks.Api.Application.Validation.Communications;

public class PutCommandValidator : AbstractValidator<PutCommand<CommunicationResource>>
{
    public PutCommandValidator(IClientRepository clientRepository)
    {
        var ownerRule = new CommunicationOwnerRule(clientRepository);

        RuleFor(x => x.Resource)
            .MustAsync(ownerRule.IsAcceptableAsync)
            .WithMessage(ClientTypeValidator.StandInCommunicationsMessage);
    }
}
