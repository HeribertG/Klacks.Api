// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Assistant;

/// <summary>
/// Installation-wide setup snapshot along the order -> shift -> assignment chain, exposed over HTTP
/// so the frontend can tell "this installation is empty" apart from "the order list is filtered to
/// nothing right now" — a distinction <c>maxItems</c> alone cannot make.
/// </summary>
public class ScheduleSetupStateResource
{
    /// <summary>
    /// True when at least one order (open or sealed) exists.
    /// </summary>
    public bool HasOrders { get; set; }

    /// <summary>
    /// True when at least one staffable shift exists.
    /// </summary>
    public bool HasShifts { get; set; }

    /// <summary>
    /// True when at least one work assignment exists.
    /// </summary>
    public bool HasWork { get; set; }

    /// <summary>
    /// True when at least one client of type Customer exists.
    /// </summary>
    public bool HasCustomers { get; set; }

    /// <summary>
    /// True when at least one group exists.
    /// </summary>
    public bool HasGroups { get; set; }
}
