// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using FluentValidation;
using Klacks.Api.Application.Commands.Accounts;

namespace Klacks.Api.Application.Validation.Accounts;

public class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.ChangePassword.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email must be a valid email address.");

        RuleFor(x => x.ChangePassword.OldPassword)
            .NotEmpty().WithMessage("Current password is required.");

        RuleFor(x => x.ChangePassword.Password)
            .NotEmpty().WithMessage("New password is required.")
            .MinimumLength(8).WithMessage("New password must be at least 8 characters long.")
            .Matches(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]")
            .WithMessage("New password must contain at least one uppercase letter, one lowercase letter, one digit, and one special character.");

        RuleFor(x => x.ChangePassword.Password)
            .NotEqual(x => x.ChangePassword.OldPassword)
            .WithMessage("New password must be different from the current password.");
    }
}