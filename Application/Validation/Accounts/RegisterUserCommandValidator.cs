// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Shape checks for a registration from the user administration. Password rules and the e-mail format are
/// deliberately NOT checked here: Identity's password policy and RegisterUserCommandHandler decide them and
/// answer with reason codes the UI translates. A second, stricter copy here once rejected passwords the published
/// policy allows.
/// </summary>

using FluentValidation;
using Klacks.Api.Application.Commands.Accounts;
using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Application.Validation.Accounts;

public class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    private const int MaxEmailLength = 255;
    private const int MinNameLength = 2;
    private const int MaxNameLength = 100;
    private const int MinUserNameLength = 3;
    private const int MaxUserNameLength = 50;
    private const string UserNamePattern = @"^[a-zA-Z0-9_-]+$";

    public RegisterUserCommandValidator()
    {
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.Registration.Email)
            .MaximumLength(MaxEmailLength).WithMessage($"The e-mail address must not exceed {MaxEmailLength} characters.");

        RuleFor(x => x.Registration.FirstName)
            .NotEmpty().WithMessage(AccountRegistrationMessages.FirstNameRequired)
            .MinimumLength(MinNameLength).WithMessage($"The first name must be at least {MinNameLength} characters long.")
            .MaximumLength(MaxNameLength).WithMessage($"The first name must not exceed {MaxNameLength} characters.");

        RuleFor(x => x.Registration.LastName)
            .NotEmpty().WithMessage(AccountRegistrationMessages.LastNameRequired)
            .MinimumLength(MinNameLength).WithMessage($"The last name must be at least {MinNameLength} characters long.")
            .MaximumLength(MaxNameLength).WithMessage($"The last name must not exceed {MaxNameLength} characters.");

        RuleFor(x => x.Registration.UserName)
            .NotEmpty().WithMessage("The user name is required.")
            .MinimumLength(MinUserNameLength).WithMessage($"The user name must be at least {MinUserNameLength} characters long.")
            .MaximumLength(MaxUserNameLength).WithMessage($"The user name must not exceed {MaxUserNameLength} characters.")
            .Matches(UserNamePattern).WithMessage("The user name may only contain letters, digits, underscores and hyphens.");
    }
}
