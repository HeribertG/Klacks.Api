using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Interfaces;

public interface IWorkLockLevelService
{
    bool CanModifyWork(WorkLockLevel effectiveLevel, bool isAdmin);
    bool CanConfirm(WorkLockLevel effectiveLevel);
    bool CanUnconfirm(WorkLockLevel effectiveLevel, bool isAdmin);
    bool CanApprove(WorkLockLevel effectiveLevel, bool isAuthorised);
    bool CanRevokeApproval(WorkLockLevel effectiveLevel, bool isAdmin);
    bool CanClosePeriod(bool isAdmin);
    bool CanReopenPeriod(bool isAdmin);
}
