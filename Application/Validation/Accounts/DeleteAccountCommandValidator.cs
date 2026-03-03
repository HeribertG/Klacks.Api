// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using FluentValidation;
using Klacks.Api.Application.Commands.Accounts;
using System.Security.Claims;

namespace Klacks.Api.Application.Validation.Accounts;

public class DeleteAccountCommandValidator : AbstractValidator<DeleteAccountCommand>
{
    public DeleteAccountCommandValidator(IHttpContextAccessor httpContextAccessor)
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
            .WithMessage("You cannot delete your own account.");
    }
}
