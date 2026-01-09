using FluentValidation;
using Klacks.Api.Application.Queries.Accounts;

namespace Klacks.Api.Application.Validation.Accounts;

public class LoginUserQueryValidator : AbstractValidator<LoginUserQuery>
{
    public LoginUserQueryValidator()
    {
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email or username is required.")
            .MaximumLength(255).WithMessage("Email or username must not exceed 255 characters.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(1).WithMessage("Password cannot be empty.");
    }
}