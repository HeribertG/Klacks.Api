// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using FluentValidation;
using Klacks.Api.Application.Commands.Accounts;
using System.Security.Claims;

namespace Klacks.Api.Application.Validation.Accounts;

/// <summary>
/// Mirrors DeleteAccountCommandValidator: an administrator must not lock themselves out of the
/// very screen that could undo it. There is deliberately no counterpart for reactivation — nobody
/// can reactivate their own account anyway, because a deactivated account cannot sign in.
/// </summary>
/// <param name="httpContextAccessor">Supplies the acting administrator's user id for the self-check.</param>
public class DeactivateAccountCommandValidator : AbstractValidator<DeactivateAccountCommand>
{
    public DeactivateAccountCommandValidator(IHttpContextAccessor httpContextAccessor)
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required.")
            .Must(userId =>
            {
                var currentUserId = httpContextAccessor.HttpContext?.User
                    .FindFirst(ClaimTypes.NameIdentifier)?.Value;

                return currentUserId == null || userId.ToString() != currentUserId;
            })
            .WithMessage("You cannot deactivate your own account.");
    }
}
