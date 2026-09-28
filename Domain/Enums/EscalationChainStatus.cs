// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Enums;

public enum EscalationChainStatus
{
    Running = 0,
    Acknowledged = 1,
    Exhausted = 2,
    Superseded = 3,
    Cancelled = 4
}
