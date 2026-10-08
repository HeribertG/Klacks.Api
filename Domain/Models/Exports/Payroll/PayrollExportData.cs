// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Root export data model for an employee-centric, day-granular payroll export of a closed period.
/// Unlike OrderExportData (order/booking centric) this groups closed time values per employee and day
/// so a country-pack formatter can emit one line per employee, day and wage kind.
/// @param StartDate - Lower bound (inclusive) of the closed period
/// @param EndDate - Upper bound (inclusive) of the closed period
/// @param ExportDate - Timestamp when the export was generated
/// @param Employees - One entry per employee with their day-granular payroll rows
/// </summary>
namespace Klacks.Api.Domain.Models.Exports.Payroll;

public class PayrollExportData
{
    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public DateTime ExportDate { get; set; } = DateTime.UtcNow;

    public List<PayrollEmployee> Employees { get; set; } = [];
}
