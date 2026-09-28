// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Enums;

public enum InboundClarificationStatus
{
    Open = 0,
    Answered = 1,
    Unresolved = 2,
    Expired = 3,
    TakenOver = 4,
    Suggested = 5
}
