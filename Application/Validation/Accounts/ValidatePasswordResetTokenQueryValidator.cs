// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using FluentValidation;
using Klacks.Api.Application.Queries.Accounts;

namespace Klacks.Api.Application.Validation.Accounts;

public class ValidatePasswordResetTokenQueryValidator : AbstractValidator<ValidatePasswordResetTokenQuery>
{
    public ValidatePasswordResetTokenQueryValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty()
            .WithMessage("Token is required.")
            .MinimumLength(10)
            .WithMessage("Invalid token format.");
    }
}