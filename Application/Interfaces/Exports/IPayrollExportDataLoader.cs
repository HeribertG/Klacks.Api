// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Loads the closed time values of a period into a person-based, day-granular payroll model. Not group-scoped:
/// each person with closed entries appears once, whatever groups the person belongs to.
/// @param fromDate - Lower bound (inclusive) for CurrentDate
/// @param untilDate - Upper bound (inclusive) for CurrentDate
/// @param clientIds - Optional restriction to these persons; null loads every person
/// </summary>
using Klacks.Api.Domain.Models.Exports.Payroll;

namespace Klacks.Api.Application.Interfaces.Exports;

public interface IPayrollExportDataLoader
{
    Task<PayrollExportData> LoadAsync(
        DateOnly fromDate,
        DateOnly untilDate,
        IReadOnlyCollection<Guid>? clientIds,
        CancellationToken cancellationToken = default);
}
