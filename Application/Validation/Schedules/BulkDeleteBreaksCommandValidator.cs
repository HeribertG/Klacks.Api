// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using FluentValidation;
using Klacks.Api.Application.Commands.Breaks;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;

namespace Klacks.Api.Application.Validation.Schedules;

public class BulkDeleteBreaksCommandValidator : AbstractValidator<BulkDeleteBreaksCommand>
{
    public BulkDeleteBreaksCommandValidator(
        IBreakRepository breakRepository,
        IWorkLockLevelService lockLevelService,
        IHttpContextAccessor httpContextAccessor)
    {
        RuleFor(x => x.Request.BreakIds)
            .MustAsync(async (breakIds, cancellationToken) =>
            {
                var isAdmin = httpContextAccessor.HttpContext?.User?.IsInRole(Roles.Admin) == true;
                if (lockLevelService.CanModifyWork(WorkLockLevel.Closed, isAdmin))
                    return true;

                var breaks = await breakRepository.GetByIdsAsync(breakIds);
                return !breaks.Any(b => b.LockLevel != WorkLockLevel.None);
            })
            .WithMessage("Cannot delete sealed break entries.");
    }
}
