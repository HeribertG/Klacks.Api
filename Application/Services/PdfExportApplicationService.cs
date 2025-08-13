using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Infrastructure.Interfaces;
using Klacks.Api.Presentation.DTOs.Filter;
using Klacks.Api.Presentation.DTOs.PdfExports;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Services;

public class PdfExportApplicationService
{
    private readonly IClientRepository _clientRepository;
    private readonly IAbsenceRepository _absenceRepository;
    private readonly IGanttPdfExportService _ganttPdfExportService;
    private readonly IMapper _mapper;
    private readonly ILogger<PdfExportApplicationService> _logger;

    public PdfExportApplicationService(
        IClientRepository clientRepository,
        IAbsenceRepository absenceRepository,
        IGanttPdfExportService ganttPdfExportService,
        IMapper mapper,
        ILogger<PdfExportApplicationService> logger)
    {
        _clientRepository = clientRepository;
        _absenceRepository = absenceRepository;
        _ganttPdfExportService = ganttPdfExportService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<GanttPdfExportResponse> GenerateGanttPdfAsync(int year, string pageFormat, string language, CancellationToken cancellationToken = default)
    {
        try
        {
            var filter = new BreakFilter
            {
                CurrentYear = year,
                SortOrder = "asc",
                OrderBy = "name",
            };

            var clients = await _clientRepository.BreakList(filter);
            var absences = await _absenceRepository.List();

            var pdfBytes = _ganttPdfExportService.GeneratePdf(
                year,
                pageFormat,
                clients,
                absences,
                language);

            return new GanttPdfExportResponse
            {
                PdfContent = pdfBytes,
                FileName = $"abwesenheit_gantt_{year}.pdf",
                ContentType = "application/pdf",
                Success = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating PDF for year {Year}", year);
            return new GanttPdfExportResponse
            {
                Success = false,
                ErrorMessage = $"Fehler beim Generieren des PDFs: {ex.Message}"
            };
        }
    }
}