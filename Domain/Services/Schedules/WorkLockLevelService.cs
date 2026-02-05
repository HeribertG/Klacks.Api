using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;

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
}
