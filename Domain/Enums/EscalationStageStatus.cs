// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Enums;

public enum EscalationStageStatus
{
    Pending = 0,
    Notified = 1,
    Acknowledged = 2,
    Declined = 3,
    Expired = 4,
    Skipped = 5,
    Cancelled = 6
}
