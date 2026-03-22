// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Statistics resource for shift coverage and sealing status per group.
/// </summary>
/// <param name="GroupId">ID of the group</param>
/// <param name="GroupName">Name of the group</param>
/// <param name="TotalSlots">Sum of quantity across all shift days</param>
/// <param name="CoveredSlots">Sum of engaged slots</param>
/// <param name="TotalWorkEntries">Total number of work entries in the period</param>
/// <param name="SealedWorkEntries">Number of work entries with LockLevel >= Confirmed</param>
namespace Klacks.Api.Application.DTOs.Dashboard;

public class ShiftCoverageStatisticsResource
{
    public Guid GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public int TotalSlots { get; set; }
    public int CoveredSlots { get; set; }
    public int TotalWorkEntries { get; set; }
    public int SealedWorkEntries { get; set; }
}
