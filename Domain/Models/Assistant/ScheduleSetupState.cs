// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Snapshot of how far an installation has been set up along the order -> shift -> assignment chain,
/// plus the two prerequisites a duty cannot be created without. Read once per detector tick so a
/// trigger can tell "nothing exists yet" apart from "shifts exist but nobody is assigned", which are
/// two different pieces of advice for a planner.
/// </summary>
/// <param name="HasOrders">True when at least one order (open or sealed) exists.</param>
/// <param name="HasShifts">True when at least one staffable shift exists.</param>
/// <param name="HasWork">True when at least one work assignment exists.</param>
/// <param name="HasCustomers">True when at least one client of type Customer exists.</param>
/// <param name="HasGroups">True when at least one group exists.</param>

namespace Klacks.Api.Domain.Models.Assistant;

public sealed record ScheduleSetupState(
    bool HasOrders,
    bool HasShifts,
    bool HasWork,
    bool HasCustomers,
    bool HasGroups);
