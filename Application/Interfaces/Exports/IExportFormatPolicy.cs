// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Central authority for which export formats are available and enabled, spanning both the
/// order/Fibu family (IExportFormatter) and the payroll/country-pack family
/// (IPayrollExportFormatter) in one combined catalog. Fixed formats (csv/json/xml) are always
/// enabled; every other registered format (order or payroll) is optional and governed by the
/// ENABLED_EXPORT_FORMATS setting.
/// </summary>
using Klacks.Api.Application.DTOs.Exports;

namespace Klacks.Api.Application.Interfaces.Exports;

public interface IExportFormatPolicy
{
    Task<IReadOnlyList<ExportFormatResource>> GetCatalogAsync(CancellationToken cancellationToken);

    Task<bool> IsEnabledAsync(string formatKey, CancellationToken cancellationToken);
}
