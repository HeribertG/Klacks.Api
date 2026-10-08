// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Default IParentWorkLockGuard. A legacy period close seals Works without SealedDay rows, so the day lock does
/// not cover them; this guard closes that gap by checking the Work's own lock level, for writes to the Work itself
/// and for writes to its child entries. Both share one rule and differ only in the refusal message.
/// </summary>
/// <param name="workLockLevelService">Decides which roles may lift a Confirmed or Approved lock</param>

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Services.Schedules;

public class ParentWorkLockGuard : IParentWorkLockGuard
{
    public const string ClosedParentMessage =
        "Cannot modify an entry of a work in a closed period. Reopen the period first.";

    public const string SealedParentMessage =
        "Cannot modify an entry of a sealed work without the role required to unseal it.";

    public const string ClosedWorkMessage =
        "Cannot modify a work in a closed period. Reopen the period first.";

    public const string SealedWorkMessage =
        "Cannot modify a sealed work without the role required to unseal it.";

    private readonly IWorkLockLevelService _workLockLevelService;

    public ParentWorkLockGuard(IWorkLockLevelService workLockLevelService)
    {
        _workLockLevelService = workLockLevelService;
    }

    public void EnsureChildWritable(Work parentWork, bool isAdmin, bool isAuthorised)
        => EnsureWritable(parentWork, isAdmin, isAuthorised, ClosedParentMessage, SealedParentMessage);

    public void EnsureWorkWritable(Work work, bool isAdmin, bool isAuthorised)
        => EnsureWritable(work, isAdmin, isAuthorised, ClosedWorkMessage, SealedWorkMessage);

    private void EnsureWritable(Work work, bool isAdmin, bool isAuthorised, string closedMessage, string sealedMessage)
    {
        if (work.AnalyseToken.HasValue || work.LockLevel == WorkLockLevel.None)
        {
            return;
        }

        if (work.LockLevel == WorkLockLevel.Closed)
        {
            throw new InvalidRequestException(closedMessage);
        }

        if (!_workLockLevelService.CanUnseal(work.LockLevel, isAdmin, isAuthorised))
        {
            throw new InvalidRequestException(sealedMessage);
        }
    }
}
