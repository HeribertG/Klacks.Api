using Klacks.Api.Application.Queries.Absences;
using Klacks.Api.Application.Services;
using Klacks.Api.Presentation.DTOs;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Handlers.Absences;

public class CreateExcelFileQueryHandler : IRequestHandler<CreateExcelFileQuery, HttpResultResource>
{
    private readonly AbsenceApplicationService _absenceApplicationService;
    private readonly ILogger<CreateExcelFileQueryHandler> _logger;

    public CreateExcelFileQueryHandler(
        AbsenceApplicationService absenceApplicationService,
        ILogger<CreateExcelFileQueryHandler> logger)
    {
        _absenceApplicationService = absenceApplicationService;
        _logger = logger;
    }

    public async Task<HttpResultResource> Handle(CreateExcelFileQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Processing Excel file creation for language: {Language}", request.Language);
            
            var result = await Task.FromResult(_absenceApplicationService.CreateExcelFileAsync(request.Language, cancellationToken));
            
            _logger.LogInformation("Excel file creation processed successfully");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Excel file creation for language: {Language}", request.Language);
            throw;
        }
    }
}
