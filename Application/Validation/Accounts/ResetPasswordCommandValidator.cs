using FluentValidation;
using Klacks.Api.Application.Commands.Accounts;

namespace Klacks.Api.Application.Validation.Accounts;

public class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(x => x.ResetPassword.Token)
            .NotEmpty()
            .WithMessage("Reset token is required.");

        RuleFor(x => x.ResetPassword.Password)
            .NotEmpty()
            .WithMessage("Password is required.")
            .MinimumLength(8)
            .WithMessage("Password must be at least 8 characters long.")
            .Matches(@"^(?=.*[0-9])(?=.*[!@#$%^&*()_+\-=\[\]{};':""\\|,.<>\/?]).*$")
            .WithMessage("Password must contain at least one digit and one special character.");
    }
}