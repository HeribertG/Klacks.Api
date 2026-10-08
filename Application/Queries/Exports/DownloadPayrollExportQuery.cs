// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Query for the stored artifact of an earlier payroll export run.
/// @param ExportLogId - The ExportLog row of the run
/// </summary>
using Klacks.Api.Application.DTOs.Exports;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Exports;

public record DownloadPayrollExportQuery(Guid ExportLogId) : IRequest<PayrollExportOutcome>;
