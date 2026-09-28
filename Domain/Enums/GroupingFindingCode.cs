// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Enums;

public enum GroupingFindingCode
{
    ShiftUnfillableGlobally = 1,
    ShiftUncoveredInGroup = 2,
    ShiftWithoutGroup = 3,
    ClientWithoutGroup = 4,
    ClientFitsNoShift = 5,
    ClientDeadMembership = 6,
    CapacityShortfall = 7,
}
