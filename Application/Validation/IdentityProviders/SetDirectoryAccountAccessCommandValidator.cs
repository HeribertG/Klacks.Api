// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using FluentValidation;
using Klacks.Api.Application.Commands.IdentityProviders;
using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Application.Validation.IdentityProviders;

/// <summary>
/// Requires a provider id and a non-empty external id within the stored column length.
/// </summary>
public class SetDirectoryAccountAccessCommandValidator : AbstractValidator<SetDirectoryAccountAccessCommand>
{
    public SetDirectoryAccountAccessCommandValidator()
    {
        RuleFor(x => x.ProviderId)
            .NotEmpty()
            .WithMessage("Identity provider ID is required.");

        RuleFor(x => x.ExternalId)
            .NotEmpty()
            .WithMessage("The directory account ID is required.")
            .MaximumLength(DirectoryAccountLimits.ExternalIdMaxLength)
            .WithMessage($"The directory account ID must not exceed {DirectoryAccountLimits.ExternalIdMaxLength} characters.");
    }
}
