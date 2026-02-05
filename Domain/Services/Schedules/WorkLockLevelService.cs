using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;

namespace Klacks.Api.Domain.Services.Schedules;

public class WorkLockLevelService : IWorkLockLevelService
{
    public bool CanModifyWork(WorkLockLevel effectiveLevel, bool isAdmin)
    {
        if (isAdmin) return true;
        return effectiveLevel == WorkLockLevel.None;
    }

    public bool CanConfirm(WorkLockLevel effectiveLevel)
    {
        return effectiveLevel == WorkLockLevel.None;
    }

    public bool CanUnconfirm(WorkLockLevel effectiveLevel, bool isAdmin)
    {
        if (isAdmin) return effectiveLevel <= WorkLockLevel.Confirmed;
        return effectiveLevel == WorkLockLevel.Confirmed;
    }

    public bool CanApprove(WorkLockLevel effectiveLevel, bool isAuthorised)
    {
        return isAuthorised && effectiveLevel <= WorkLockLevel.Confirmed;
    }

    public bool CanRevokeApproval(WorkLockLevel effectiveLevel, bool isAdmin)
    {
        return isAdmin && effectiveLevel == WorkLockLevel.Approved;
    }

    public bool CanClosePeriod(bool isAdmin)
    {
        return isAdmin;
    }

    public bool CanReopenPeriod(bool isAdmin)
    {
        return isAdmin;
    }
}
