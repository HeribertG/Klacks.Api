// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Single work entry within an order export, including related changes, expenses and breaks.
/// @param WorkId - ID of the work entry
/// @param EmployeeName - Full name of the assigned employee
/// @param WorkDate - The date this work was performed
/// </summary>
namespace Klacks.Api.Domain.Models.Exports;

public class WorkExportEntry
{
    public Guid WorkId { get; set; }

    public Guid EmployeeId { get; set; }

    public string EmployeeName { get; set; } = string.Empty;

    public int EmployeeIdNumber { get; set; }

    public DateOnly WorkDate { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public decimal WorkTime { get; set; }

    public decimal Surcharges { get; set; }

    public string? Information { get; set; }

    public List<WorkChangeExportEntry> Changes { get; set; } = [];

    public List<ExpensesExportEntry> Expenses { get; set; } = [];

    public List<BreakExportEntry> Breaks { get; set; } = [];
}
