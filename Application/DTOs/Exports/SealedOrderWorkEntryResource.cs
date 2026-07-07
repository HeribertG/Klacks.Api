// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Single work entry within the sealed order detail preview, carrying the lock level
/// and the period-closed flag so the user can see which entries an export would include.
/// @param LockLevel - Numeric WorkLockLevel of the entry (0=None .. 3=Closed)
/// @param PeriodClosed - True when the (employee, date) pair falls into a closed period
/// </summary>
namespace Klacks.Api.Application.DTOs.Exports;

public class SealedOrderWorkEntryResource
{
    public Guid WorkId { get; set; }

    public Guid EmployeeId { get; set; }

    public string EmployeeName { get; set; } = string.Empty;

    public int EmployeeIdNumber { get; set; }

    public DateOnly WorkDate { get; set; }

    public string StartTime { get; set; } = string.Empty;

    public string EndTime { get; set; } = string.Empty;

    public decimal Hours { get; set; }

    public decimal Surcharges { get; set; }

    public int LockLevel { get; set; }

    public bool PeriodClosed { get; set; }
}
