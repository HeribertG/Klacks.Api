// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Loads the employee-centric, day-granular payroll data of a closed group period.
/// </summary>
/// <param name="dataLoader">Loads and projects the closed work/break entries of the group period</param>
using Klacks.Api.Application.Interfaces.Exports;
using Klacks.Api.Application.Queries.Exports;
using Klacks.Api.Domain.Models.Exports.Payroll;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Exports;

public class GetPayrollPeriodDataQueryHandler : IRequestHandler<GetPayrollPeriodDataQuery, PayrollExportData>
{
    private readonly IPayrollExportDataLoader _dataLoader;

    public GetPayrollPeriodDataQueryHandler(IPayrollExportDataLoader dataLoader)
    {
        _dataLoader = dataLoader;
    }

    public async Task<PayrollExportData> Handle(GetPayrollPeriodDataQuery request, CancellationToken cancellationToken)
    {
        return await _dataLoader.LoadAsync(request.GroupId, request.StartDate, request.EndDate, cancellationToken);
    }
}
