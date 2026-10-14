// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Enums;

public enum ReplacementSlotStatus
{
    Covered = 0,
    AwaitingAnswer = 1,
    NobodyAvailable = 2,
    NotYetAsked = 3,
    PartlyDeclined = 4,
}
