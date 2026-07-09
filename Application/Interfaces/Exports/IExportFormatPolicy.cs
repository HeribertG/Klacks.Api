// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Central authority for which order export formats are available and enabled.
/// Fixed formats (csv/json/xml) are always enabled; optional formats are governed
/// by the ENABLED_EXPORT_FORMATS setting.
/// </summary>
using Klacks.Api.Application.DTOs.Exports;

namespace Klacks.Api.Application.Interfaces.Exports;

public interface IExportFormatPolicy
{
    Task<IReadOnlyList<ExportFormatResource>> GetCatalogAsync(CancellationToken cancellationToken);

    Task<bool> IsEnabledAsync(string formatKey, CancellationToken cancellationToken);
}
