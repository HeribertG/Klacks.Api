// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Enums;

public enum WorkBookingRefusalReason
{
    DaySealed = 0,
    OutsideMembershipPeriod = 1,
    OutsideGroupPeriod = 2,
    SporadicShiftDayFull = 3,
    SporadicShiftRangeExhausted = 4,
    HardBlockingConflict = 5,
    AssistantOnlyConflict = 6,
    CellOccupiedByWork = 7,
    CellOccupiedByAbsence = 8
}
