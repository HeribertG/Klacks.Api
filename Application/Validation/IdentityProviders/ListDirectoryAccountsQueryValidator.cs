// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using FluentValidation;
using Klacks.Api.Application.Queries.IdentityProviders;
using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Application.Validation.IdentityProviders;

/// <summary>
/// Requires a provider id and caps the search term length.
/// </summary>
public class ListDirectoryAccountsQueryValidator : AbstractValidator<ListDirectoryAccountsQuery>
{
    public ListDirectoryAccountsQueryValidator()
    {
        RuleFor(x => x.ProviderId)
            .NotEmpty()
            .WithMessage("Identity provider ID is required.");

        RuleFor(x => x.Search)
            .MaximumLength(DirectoryAccountLimits.SearchMaxLength)
            .WithMessage($"The search term must not exceed {DirectoryAccountLimits.SearchMaxLength} characters.");
    }
}
