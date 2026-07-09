// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Returns the order export format catalog with each format's fixed and enabled state.
/// </summary>
using Klacks.Api.Application.DTOs.Exports;
using Klacks.Api.Application.Interfaces.Exports;
using Klacks.Api.Application.Queries.Exports;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Exports;

public class ListExportFormatsQueryHandler : IRequestHandler<ListExportFormatsQuery, IReadOnlyList<ExportFormatResource>>
{
    private readonly IExportFormatPolicy _exportFormatPolicy;

    public ListExportFormatsQueryHandler(IExportFormatPolicy exportFormatPolicy)
    {
        _exportFormatPolicy = exportFormatPolicy;
    }

    public async Task<IReadOnlyList<ExportFormatResource>> Handle(ListExportFormatsQuery request, CancellationToken cancellationToken)
    {
        return await _exportFormatPolicy.GetCatalogAsync(cancellationToken);
    }
}
