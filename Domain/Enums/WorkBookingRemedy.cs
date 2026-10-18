// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Enums;

public enum WorkBookingRemedy
{
    ReopenPeriodAdmin = 0,
    RevokeDayApproval = 1,
    AdjustMembership = 2,
    AdjustGroupMembership = 3,
    ChooseOtherDayOrShift = 4,
    AddQualificationOrRelaxLevel = 5,
    AdjustPlanOrEnterDirectly = 6,
    RemoveOrMoveExistingEntry = 7,
    RemoveConfirmationFirst = 8,
    AskForGroupVisibility = 9
}
