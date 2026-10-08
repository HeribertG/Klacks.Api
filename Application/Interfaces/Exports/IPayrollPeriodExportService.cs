// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The single path for payroll exports of a period: preview and run. Any trigger (manual request today, an
/// automatic export on period close later) goes through ExportAsync. Throws PayrollExportBlockedException when the
/// period is not complete, PayrollExportNothingNewException when every person was already exported unchanged and
/// PayrollExportConcurrentException when another export won the race.
/// @param from - First day of the period (inclusive)
/// @param until - Last day of the period (inclusive)
/// @param format - Payroll FormatKey selecting the formatter
/// @param language - Culture name stored with the export log
/// @param clientIds - Optional restriction to these persons (supplementary export); null covers everybody, an empty list is rejected
/// @param actor - Identifier of the user who triggers the export, stored as ExportedBy
/// </summary>
using Klacks.Api.Application.DTOs.Exports;

namespace Klacks.Api.Application.Interfaces.Exports;

public interface IPayrollPeriodExportService
{
    Task<PayrollExportPreviewDto> PreviewAsync(
        DateOnly from,
        DateOnly until,
        string format,
        IReadOnlyCollection<Guid>? clientIds,
        CancellationToken cancellationToken = default);

    Task<PayrollExportOutcome> ExportAsync(
        DateOnly from,
        DateOnly until,
        string format,
        string language,
        IReadOnlyCollection<Guid>? clientIds,
        string actor,
        CancellationToken cancellationToken = default);
}
