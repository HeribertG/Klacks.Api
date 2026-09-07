// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Validates the undo of a Work delete for the callers who are allowed to know about it: a sealed Work
/// may only be restored by an Admin, and the deleting user only inside the undo window. A foreign delete
/// (IWorkRestoreAuthorizer answers Hidden) is deliberately NOT judged here - it passes through so the
/// handler answers 404, and no 400 message can reveal that the Work exists. A missing or not-deleted
/// Work passes for the same reason.
/// </summary>
/// <param name="workRepository">Source of the soft-deleted Work including its delete stamp</param>
/// <param name="lockLevelService">Decides whether the Work's lock level admits a modification</param>
/// <param name="authorizer">Resolves whether the caller is Admin, the deleting user, or neither</param>
/// <param name="timeProvider">Clock the undo window is measured against</param>

using FluentValidation;
using Klacks.Api.Application.Commands.Works;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.Validation.Schedules;

public class RestoreWorkCommandValidator : AbstractValidator<RestoreWorkCommand>
{
    public const string SealedWorkMessage = "Cannot restore a sealed work entry.";
    public const string UndoWindowExpiredMessage = "The undo window for this work entry has expired.";

    public RestoreWorkCommandValidator(
        IWorkRepository workRepository,
        IWorkLockLevelService lockLevelService,
        IWorkRestoreAuthorizer authorizer,
        TimeProvider timeProvider)
    {
        RuleFor(x => x.Id)
            .CustomAsync(async (id, context, cancellationToken) =>
            {
                var work = await workRepository.GetDeletedAsync(id, cancellationToken);
                if (work == null)
                {
                    return;
                }

                var access = authorizer.Resolve(work);
                if (access == WorkRestoreAccess.Hidden)
                {
                    return;
                }

                var isAdmin = access == WorkRestoreAccess.Admin;
                if (!lockLevelService.CanModifyWork(work.LockLevel, isAdmin))
                {
                    context.AddFailure(SealedWorkMessage);
                    return;
                }

                if (isAdmin)
                {
                    return;
                }

                if (!IsInsideUndoWindow(work, timeProvider))
                {
                    context.AddFailure(UndoWindowExpiredMessage);
                }
            });
    }

    private static bool IsInsideUndoWindow(Work work, TimeProvider timeProvider)
    {
        if (!work.DeletedTime.HasValue)
        {
            return false;
        }

        var elapsed = timeProvider.GetUtcNow().UtcDateTime - work.DeletedTime.Value;
        return elapsed <= TimeSpan.FromSeconds(WorkRestoreDefaults.UndoWindowSeconds);
    }
}
