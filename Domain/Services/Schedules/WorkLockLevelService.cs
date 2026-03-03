// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Services.Schedules;

public class WorkLockLevelService : IWorkLockLevelService
{
    public bool CanModifyWork(WorkLockLevel level, bool isAdmin)
    {
        if (isAdmin) return true;
        return level == WorkLockLevel.None;
    }

    public bool CanSeal(WorkLockLevel currentLevel, WorkLockLevel targetLevel, bool isAdmin, bool isAuthorised)
    {
        if (targetLevel <= currentLevel) return false;

        return targetLevel switch
        {
            WorkLockLevel.Confirmed => true,
            WorkLockLevel.Approved => isAuthorised || isAdmin,
            WorkLockLevel.Closed => isAdmin,
            _ => false
        };
    }

    public bool CanUnseal(WorkLockLevel currentLevel, bool isAdmin, bool isAuthorised)
    {
        if (currentLevel == WorkLockLevel.None) return false;

        return currentLevel switch
        {
            WorkLockLevel.Confirmed => true,
            WorkLockLevel.Approved => isAuthorised || isAdmin,
            WorkLockLevel.Closed => isAdmin,
            _ => false
        };
    }

    public void Seal(ScheduleEntryBase entity, WorkLockLevel targetLevel, string userName, bool isAdmin, bool isAuthorised)
    {
        if (!CanSeal(entity.LockLevel, targetLevel, isAdmin, isAuthorised))
            throw new InvalidRequestException("Entry cannot be sealed to the requested level.");

        entity.LockLevel = targetLevel;
        entity.SealedAt = DateTime.UtcNow;
        entity.SealedBy = userName;
    }

    public void Unseal(ScheduleEntryBase entity, bool isAdmin, bool isAuthorised)
    {
        if (!CanUnseal(entity.LockLevel, isAdmin, isAuthorised))
            throw new InvalidRequestException("Entry cannot be unsealed in its current state.");

        entity.LockLevel = WorkLockLevel.None;
        entity.SealedAt = null;
        entity.SealedBy = null;
    }
}
