// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

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
