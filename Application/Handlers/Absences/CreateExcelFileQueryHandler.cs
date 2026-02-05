using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Absences;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Application.DTOs;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Handlers.Absences;

public class CreateExcelFileQueryHandler : IRequestHandler<CreateExcelFileQuery, HttpResultResource>
{
    private readonly IAbsenceRepository _absenceRepository;
    private readonly ILogger<CreateExcelFileQueryHandler> _logger;

    public CreateExcelFileQueryHandler(
        IAbsenceRepository absenceRepository,
        ILogger<CreateExcelFileQueryHandler> logger)
    {
        _absenceRepository = absenceRepository;
        _logger = logger;
    }

    public async Task<HttpResultResource> Handle(CreateExcelFileQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Processing Excel file creation for language: {Language}", request.Language);
            
            if (string.IsNullOrWhiteSpace(request.Language))
            {
                _logger.LogWarning("Invalid language parameter provided for Excel file creation");
                throw new InvalidRequestException("Language parameter cannot be empty");
            }
            
            var result = await Task.FromResult(_absenceRepository.CreateExcelFile(request.Language));
            
            if (!result.Success)
            {
                _logger.LogWarning("Failed to create Excel file: {Message}", result.Messages);
                throw new InvalidRequestException($"Failed to create Excel file: {result.Messages}");
            }
            
            _logger.LogInformation("Excel file creation processed successfully");
            return result;
        }
        catch (InvalidRequestException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while creating Excel file for language: {Language}", request.Language);
            throw new InvalidRequestException($"Failed to create Excel file for absences: {ex.Message}");
        }
    }
}
