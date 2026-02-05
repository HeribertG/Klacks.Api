using FluentValidation;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
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
                var isAdmin = httpContextAccessor.HttpContext?.User?.IsInRole("Admin") == true;
                if (lockLevelService.CanModifyWork(WorkLockLevel.Closed, isAdmin))
                    return true;

                foreach (var id in workIds)
                {
                    var work = await workRepository.Get(id);
                    if (work != null && work.LockLevel != WorkLockLevel.None)
                        return false;
                }

                return true;
            })
            .WithMessage("Cannot delete sealed work entries.");
    }
}
