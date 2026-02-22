// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using FluentValidation;
using Klacks.Api.Application.Commands.Accounts;

namespace Klacks.Api.Application.Validation.Accounts;

public class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.Registration.Email)
            .NotEmpty().WithMessage("E-Mail-Adresse ist erforderlich.")
            .EmailAddress().WithMessage("E-Mail-Adresse ist ungültig.")
            .MaximumLength(255).WithMessage("E-Mail-Adresse darf maximal 255 Zeichen haben.");

        RuleFor(x => x.Registration.Password)
            .NotEmpty().WithMessage("Passwort ist erforderlich.")
            .MinimumLength(8).WithMessage("Passwort muss mindestens 8 Zeichen lang sein.")
            .Matches(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]")
            .WithMessage("Passwort muss mindestens einen Großbuchstaben, einen Kleinbuchstaben, eine Ziffer und ein Sonderzeichen enthalten.");

        RuleFor(x => x.Registration.FirstName)
            .NotEmpty().WithMessage("Vorname ist erforderlich.")
            .MinimumLength(2).WithMessage("Vorname muss mindestens 2 Zeichen lang sein.")
            .MaximumLength(100).WithMessage("Vorname darf maximal 100 Zeichen haben.");

        RuleFor(x => x.Registration.LastName)
            .NotEmpty().WithMessage("Nachname ist erforderlich.")
            .MinimumLength(2).WithMessage("Nachname muss mindestens 2 Zeichen lang sein.")
            .MaximumLength(100).WithMessage("Nachname darf maximal 100 Zeichen haben.");

        RuleFor(x => x.Registration.UserName)
            .NotEmpty().WithMessage("Benutzername ist erforderlich.")
            .MinimumLength(3).WithMessage("Benutzername muss mindestens 3 Zeichen lang sein.")
            .MaximumLength(50).WithMessage("Benutzername darf maximal 50 Zeichen haben.")
            .Matches(@"^[a-zA-Z0-9_-]+$").WithMessage("Benutzername darf nur Buchstaben, Zahlen, Unterstriche und Bindestriche enthalten.");
    }
}