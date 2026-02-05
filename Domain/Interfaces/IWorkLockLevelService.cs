using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Interfaces;

public interface IWorkLockLevelService
{
    bool CanModifyWork(WorkLockLevel level, bool isAdmin);
    bool CanSeal(WorkLockLevel currentLevel, WorkLockLevel targetLevel, bool isAdmin, bool isAuthorised);
    bool CanUnseal(WorkLockLevel currentLevel, bool isAdmin, bool isAuthorised);
}
