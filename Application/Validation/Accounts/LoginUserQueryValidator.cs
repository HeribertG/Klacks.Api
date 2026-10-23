// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using FluentValidation;
using Klacks.Api.Application.Queries.Accounts;

namespace Klacks.Api.Application.Validation.Accounts;

public class LoginUserQueryValidator : AbstractValidator<LoginUserQuery>
{
    private const int MaxLoginNameLength = 255;

    public LoginUserQueryValidator()
    {
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email or username is required.")
            .MaximumLength(MaxLoginNameLength).WithMessage($"Email or username must not exceed {MaxLoginNameLength} characters.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(1).WithMessage("Password cannot be empty.");
    }
}