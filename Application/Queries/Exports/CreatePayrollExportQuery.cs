// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Query to generate a manual, on-demand payroll export file for a single group and date range.
/// @param Filter - Contains the group, date range, localization and payroll format key
/// </summary>
using Klacks.Api.Application.DTOs.Exports;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Exports;

public record CreatePayrollExportQuery(PayrollExportFilter Filter) : IRequest<OrderExportResult>;
