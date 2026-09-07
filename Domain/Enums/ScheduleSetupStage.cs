// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Enums;

public enum ScheduleSetupStage
{
    NothingYet = 0,
    OrdersButNoShifts = 1,
    ShiftsButNoWork = 2,
}
