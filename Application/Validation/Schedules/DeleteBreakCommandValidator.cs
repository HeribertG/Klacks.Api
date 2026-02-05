using FluentValidation;
using Klacks.Api.Application.Commands.Breaks;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;

namespace Klacks.Api.Application.Validation.Schedules;

public class DeleteBreakCommandValidator : AbstractValidator<DeleteBreakCommand>
{
    public DeleteBreakCommandValidator(
        IBreakRepository breakRepository,
        IWorkLockLevelService lockLevelService,
        IHttpContextAccessor httpContextAccessor)
    {
        RuleFor(x => x.Id)
            .MustAsync(async (id, cancellationToken) =>
            {
                var breakEntry = await breakRepository.Get(id);
                if (breakEntry == null || breakEntry.LockLevel == WorkLockLevel.None)
                    return true;

                var isAdmin = httpContextAccessor.HttpContext?.User?.IsInRole("Admin") == true;
                return lockLevelService.CanModifyWork(breakEntry.LockLevel, isAdmin);
            })
            .WithMessage("Cannot delete a sealed break entry.");
    }
}
