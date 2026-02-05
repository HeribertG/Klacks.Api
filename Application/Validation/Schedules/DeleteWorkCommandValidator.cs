using FluentValidation;
using Klacks.Api.Application.Commands.Works;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;

namespace Klacks.Api.Application.Validation.Schedules;

public class DeleteWorkCommandValidator : AbstractValidator<DeleteWorkCommand>
{
    public DeleteWorkCommandValidator(
        IWorkRepository workRepository,
        IWorkLockLevelService lockLevelService,
        IHttpContextAccessor httpContextAccessor)
    {
        RuleFor(x => x.Id)
            .MustAsync(async (id, cancellationToken) =>
            {
                var work = await workRepository.Get(id);
                if (work == null || work.LockLevel == WorkLockLevel.None)
                    return true;

                var isAdmin = httpContextAccessor.HttpContext?.User?.IsInRole("Admin") == true;
                return lockLevelService.CanModifyWork(work.LockLevel, isAdmin);
            })
            .WithMessage("Cannot delete a sealed work entry.");
    }
}
