// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Models.Update;

public enum UpdateOperationStatus
{
    Pending = 0,
    Running = 1,
    Succeeded = 2,
    Failed = 3,
    RolledBack = 4,
    Cancelled = 5,
    RollbackFailed = 6,
}
