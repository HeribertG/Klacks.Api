using Klacks.Api.Interfaces;
using Klacks.Api.Queries.PdfExports;
using Klacks.Api.Resources.Filter;
using Klacks.Api.Resources.PdfExports;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Klacks.Api.Handlers.PdfExports;

public class GanttPdfExportQueryHandler : IRequestHandler<GanttPdfExportQuery, GanttPdfExportResponse>
{
    private readonly IClientRepository _clientRepository;
    private readonly IAbsenceRepository _absenceRepository;
    private readonly IGanttPdfExportService _ganttPdfExportService;

    public GanttPdfExportQueryHandler(
        IClientRepository clientRepository,
        IAbsenceRepository absenceRepository,
        IGanttPdfExportService ganttPdfExportService)
    {
        _clientRepository = clientRepository;
        _absenceRepository = absenceRepository;
        _ganttPdfExportService = ganttPdfExportService;
    }

    public async Task<GanttPdfExportResponse> Handle(GanttPdfExportQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // Anfrage-Parameter setzen
            var filter = new BreakFilter
            {
                CurrentYear = request.Year,
                SortOrder = "asc",
                OrderBy = "name",
            };

            // Daten aus den Repositories abrufen
            var clients = await _clientRepository.BreakList(filter);
            var absences = await _absenceRepository.List();

            // PDF über den Service generieren
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
            return new GanttPdfExportResponse
            {
                Success = false,
                ErrorMessage = $"Fehler beim Generieren des PDFs: {ex.Message}"
            };
        }
    }
}