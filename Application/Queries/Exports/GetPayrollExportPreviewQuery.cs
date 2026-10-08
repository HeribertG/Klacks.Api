// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Query for what a payroll export of a period would do right now: blockers, persons that are new or changed.
/// @param FromDate - First day of the period (inclusive)
/// @param UntilDate - Last day of the period (inclusive)
/// @param Format - Payroll FormatKey the persons are compared against
/// @param ClientIds - Optional restriction to these persons; null covers everybody
/// </summary>
using Klacks.Api.Application.DTOs.Exports;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Exports;

public record GetPayrollExportPreviewQuery(
    DateOnly FromDate,
    DateOnly UntilDate,
    string Format,
    IReadOnlyCollection<Guid>? ClientIds) : IRequest<PayrollExportPreviewDto>;
