using Klacks.Api.Application.Queries.PdfExports;
using Klacks.Api.Application.Services;
using Klacks.Api.Presentation.DTOs.PdfExports;
using MediatR;

namespace Klacks.Api.Application.Handlers.PdfExports;

public class GanttPdfExportQueryHandler : IRequestHandler<GanttPdfExportQuery, GanttPdfExportResponse>
{
    private readonly PdfExportApplicationService _pdfExportApplicationService;

    public GanttPdfExportQueryHandler(PdfExportApplicationService pdfExportApplicationService)
    {
        _pdfExportApplicationService = pdfExportApplicationService;
    }

    public async Task<GanttPdfExportResponse> Handle(GanttPdfExportQuery request, CancellationToken cancellationToken)
    {
        return await _pdfExportApplicationService.GenerateGanttPdfAsync(request.Year, request.PageFormat, request.Language, cancellationToken);
    }
}