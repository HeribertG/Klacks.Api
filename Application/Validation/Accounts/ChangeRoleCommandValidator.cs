// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using FluentValidation;
using Klacks.Api.Application.Commands.Accounts;
using System.Security.Claims;

namespace Klacks.Api.Application.Validation.Accounts;

public class ChangeRoleCommandValidator : AbstractValidator<ChangeRoleCommand>
{
    public ChangeRoleCommandValidator(IHttpContextAccessor httpContextAccessor)
    {
        RuleFor(x => x.ChangeRole.UserId)
            .NotEmpty()
            .WithMessage("User ID is required.")
            .Must(userId =>
            {
                var currentUserId = httpContextAccessor.HttpContext?.User
                    .FindFirst(ClaimTypes.NameIdentifier)?.Value;

                return currentUserId == null || userId != currentUserId;
            })
            .WithMessage("You cannot change your own role.");
    }
}
