// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Interfaces.Schedules;

public interface IWorkLockLevelService
{
    bool CanModifyWork(WorkLockLevel level, bool isAdmin);
    bool CanSeal(WorkLockLevel currentLevel, WorkLockLevel targetLevel, bool isAdmin, bool isAuthorised);
    bool CanUnseal(WorkLockLevel currentLevel, bool isAdmin, bool isAuthorised);
    void Seal(ScheduleEntryBase entity, WorkLockLevel targetLevel, string userName, bool isAdmin, bool isAuthorised);
    void Unseal(ScheduleEntryBase entity, bool isAdmin, bool isAuthorised);
}
