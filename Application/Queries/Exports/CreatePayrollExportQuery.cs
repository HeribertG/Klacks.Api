// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Query to generate a manual, on-demand payroll export file for a date range.
/// @param Filter - Contains the date range, localization, payroll format key and the optional person selection
/// </summary>
using Klacks.Api.Application.DTOs.Exports;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Exports;

public record CreatePayrollExportQuery(PayrollExportFilter Filter) : IRequest<PayrollExportOutcome>;
