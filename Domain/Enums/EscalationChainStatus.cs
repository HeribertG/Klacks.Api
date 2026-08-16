// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Enums;

public enum EscalationChainStatus
{
    Running = 0,
    Acknowledged = 1,
    Exhausted = 2,
    Superseded = 3,
    Cancelled = 4
}
