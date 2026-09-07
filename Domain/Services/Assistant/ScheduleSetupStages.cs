// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Single derivation of the setup stage from a ScheduleSetupState, shared by the detector that
/// reports the stage and the skill that explains it. Kept in one place because the two must never
/// disagree: a notification naming one stage and a guide answering for another is worse than either
/// alone. Only defined while nothing has been scheduled — once a work assignment exists there is no
/// stage left to name, which is why the caller must check HasWork first and why this throws rather
/// than inventing a fourth stage nobody renders.
/// </summary>

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Services.Assistant;

public static class ScheduleSetupStages
{
    public static ScheduleSetupStage For(ScheduleSetupState state)
    {
        if (state.HasWork)
        {
            throw new ArgumentOutOfRangeException(
                nameof(state),
                "An installation that already holds work assignments has no setup stage — check HasWork first.");
        }

        if (state.HasShifts)
        {
            return ScheduleSetupStage.ShiftsButNoWork;
        }

        return state.HasOrders
            ? ScheduleSetupStage.OrdersButNoShifts
            : ScheduleSetupStage.NothingYet;
    }
}
