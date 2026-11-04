// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Single work entry within an order export, including related changes and breaks. Expenses and
/// allowances belong to the employee, never to the customer, so they are not part of an order export
/// (they appear only in the employee-based client period export, see ClientWorkExportEntry).
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

    public List<BreakExportEntry> Breaks { get; set; } = [];
}
