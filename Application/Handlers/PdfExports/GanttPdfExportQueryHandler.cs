using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.PdfExports;
using Klacks.Api.Infrastructure.Interfaces;
using Klacks.Api.Presentation.DTOs.Filter;
using Klacks.Api.Presentation.DTOs.PdfExports;
using MediatR;

namespace Klacks.Api.Application.Handlers.PdfExports;

public class GanttPdfExportQueryHandler : IRequestHandler<GanttPdfExportQuery, GanttPdfExportResponse>
{
    private readonly IClientRepository _clientRepository;
    private readonly IAbsenceRepository _absenceRepository;
    private readonly IGanttPdfExportService _ganttPdfExportService;
    private readonly ILogger<GanttPdfExportQueryHandler> _logger;

    public GanttPdfExportQueryHandler(
        IClientRepository clientRepository,
        IAbsenceRepository absenceRepository,
        IGanttPdfExportService ganttPdfExportService,
        ILogger<GanttPdfExportQueryHandler> logger)
    {
        _clientRepository = clientRepository;
        _absenceRepository = absenceRepository;
        _ganttPdfExportService = ganttPdfExportService;
        _logger = logger;
    }

    public async Task<GanttPdfExportResponse> Handle(GanttPdfExportQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var filter = new BreakFilter
            {
                CurrentYear = request.Year,
                SortOrder = "asc",
                OrderBy = "name",
            };

            var clients = await _clientRepository.BreakList(filter);
            var absences = await _absenceRepository.List();

            var pdfBytes = _ganttPdfExportService.GeneratePdf(
                request.Year,
                request.PageFormat,
                clients,
                absences,
                request.Language);

            return new GanttPdfExportResponse
            {
                PdfContent = pdfBytes,
                FileName = $"abwesenheit_gantt_{request.Year}.pdf",
                ContentType = "application/pdf",
                Success = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating PDF for year {Year}", request.Year);
            return new GanttPdfExportResponse
            {
                Success = false,
                ErrorMessage = $"Fehler beim Generieren des PDFs: {ex.Message}"
            };
        }
    }
}