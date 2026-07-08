// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Exports.Payroll;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Exports;

public record GetPayrollPeriodDataQuery(Guid GroupId, DateOnly StartDate, DateOnly EndDate)
    : IRequest<PayrollExportData>;
