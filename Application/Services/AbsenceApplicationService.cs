using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Presentation.DTOs;
using Klacks.Api.Presentation.DTOs.Filter;
using Klacks.Api.Presentation.DTOs.Schedules;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Services;

public class AbsenceApplicationService
{
    private readonly IAbsenceRepository _absenceRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<AbsenceApplicationService> _logger;

    public AbsenceApplicationService(
        IAbsenceRepository absenceRepository,
        IMapper mapper,
        ILogger<AbsenceApplicationService> logger)
    {
        _absenceRepository = absenceRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<IEnumerable<AbsenceResource>> GetAllAbsencesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting all absences");
        var absences = await _absenceRepository.List();
        return absences.Select(a => _mapper.Map<AbsenceResource>(a));
    }

    public async Task<AbsenceResource?> GetAbsenceByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting absence by ID: {AbsenceId}", id);
        var absence = await _absenceRepository.Get(id);
        return absence != null ? _mapper.Map<AbsenceResource>(absence) : null;
    }

    public async Task<TruncatedAbsence> GetTruncatedAbsencesAsync(AbsenceFilter filter, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting truncated absences with filter");
        var result = await _absenceRepository.Truncated(filter);
        return _mapper.Map<TruncatedAbsence, TruncatedAbsence>(result!);
    }

    public async Task<AbsenceResource> CreateAbsenceAsync(AbsenceResource absenceResource, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating new absence");
        var absence = _mapper.Map<Absence>(absenceResource);
        await _absenceRepository.Add(absence);
        return _mapper.Map<AbsenceResource>(absence);
    }

    public async Task<AbsenceResource> UpdateAbsenceAsync(AbsenceResource absenceResource, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating absence: {AbsenceId}", absenceResource.Id);
        var absence = _mapper.Map<Absence>(absenceResource);
        var updatedAbsence = await _absenceRepository.Put(absence);
        return _mapper.Map<AbsenceResource>(updatedAbsence);
    }

    public async Task DeleteAbsenceAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting absence: {AbsenceId}", id);
        await _absenceRepository.Delete(id);
    }

    public HttpResultResource CreateExcelFileAsync(string language, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating Excel file for absences in language: {Language}", language);
        var result = _absenceRepository.CreateExcelFile(language);
        
        if (result.Success)
        {
            _logger.LogInformation("Excel file created successfully: {FileName}", result.Messages);
        }
        else
        {
            _logger.LogWarning("Failed to create Excel file: {Error}", result.Messages);
        }
        
        return result;
    }
}