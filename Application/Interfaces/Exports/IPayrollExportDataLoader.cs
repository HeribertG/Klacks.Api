// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Loads the closed time values of a group's period into an employee-centric, day-granular
/// payroll model. Mirrors the group scoping of the period-seal (work's shift belongs to the group,
/// no subgroup cascade) so the export contains exactly what was sealed.
/// @param groupId - The group (location/branch) whose period was closed
/// @param fromDate - Lower bound (inclusive) for CurrentDate
/// @param untilDate - Upper bound (inclusive) for CurrentDate
/// </summary>
using Klacks.Api.Domain.Models.Exports.Payroll;

namespace Klacks.Api.Application.Interfaces.Exports;

public interface IPayrollExportDataLoader
{
    Task<PayrollExportData> LoadAsync(
        Guid groupId,
        DateOnly fromDate,
        DateOnly untilDate,
        CancellationToken cancellationToken = default);
}
