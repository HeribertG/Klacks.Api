using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Application.DTOs;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Domain.Services.Absences;

public class AbsenceExportService : IAbsenceExportService
{
    private readonly ILogger<AbsenceExportService> _logger;

    public AbsenceExportService(ILogger<AbsenceExportService> logger)
    {
        this._logger = logger;
    }

    public HttpResultResource CreateExcelFile(string language)
    {
        _logger.LogInformation("Attempting to create Excel file for language: {Language}", language);
        try
        {
            bool success = false;

            if (!success)
            {
                _logger.LogError("Failed to create Excel file for language: {Language}. Simulated failure.", language);
                throw new InvalidRequestException($"Failed to create Excel file for language '{language}'.");
            }

            return new HttpResultResource { Success = true, Messages = "Excel file created successfully." };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating Excel file for language: {Language}.", language);
            throw;
        }
    }
}