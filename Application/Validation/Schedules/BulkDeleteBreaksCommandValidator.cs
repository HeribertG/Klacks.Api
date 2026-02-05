using FluentValidation;
using Klacks.Api.Application.Commands.Breaks;
using Klacks.Api.Application.Interfaces;
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
                var isAdmin = httpContextAccessor.HttpContext?.User?.IsInRole("Admin") == true;
                if (lockLevelService.CanModifyWork(WorkLockLevel.Closed, isAdmin))
                    return true;

                foreach (var id in breakIds)
                {
                    var breakEntry = await breakRepository.Get(id);
                    if (breakEntry != null && breakEntry.LockLevel != WorkLockLevel.None)
                        return false;
                }

                return true;
            })
            .WithMessage("Cannot delete sealed break entries.");
    }
}
