// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using FluentValidation;
using Klacks.Api.Application.Commands.Works;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;

namespace Klacks.Api.Application.Validation.Schedules;

public class BulkDeleteWorksCommandValidator : AbstractValidator<BulkDeleteWorksCommand>
{
    public BulkDeleteWorksCommandValidator(
        IWorkRepository workRepository,
        IWorkLockLevelService lockLevelService,
        IHttpContextAccessor httpContextAccessor)
    {
        RuleFor(x => x.Request.WorkIds)
            .MustAsync(async (workIds, cancellationToken) =>
            {
                var isAdmin = httpContextAccessor.HttpContext?.User?.IsInRole(Roles.Admin) == true;
                if (lockLevelService.CanModifyWork(WorkLockLevel.Closed, isAdmin))
                    return true;

                var works = await workRepository.GetByIdsAsync(workIds);
                return !works.Any(w => w.LockLevel != WorkLockLevel.None);
            })
            .WithMessage("Cannot delete sealed work entries.");
    }
}
