// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Queries;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Handlers.Absences;

public class GetListQueryHandler : IRequestHandler<ListQuery<AbsenceResource>, IEnumerable<AbsenceResource>>
{
    private readonly IAbsenceRepository _absenceRepository;
    private readonly SettingsMapper _settingsMapper;
    private readonly ILogger<GetListQueryHandler> _logger;

    public GetListQueryHandler(
        IAbsenceRepository absenceRepository,
        SettingsMapper settingsMapper,
        ILogger<GetListQueryHandler> logger)
    {
        _absenceRepository = absenceRepository;
        _settingsMapper = settingsMapper;
        _logger = logger;
    }

    public async Task<IEnumerable<AbsenceResource>> Handle(ListQuery<AbsenceResource> request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Processing get all absences query");

            var absences = await _absenceRepository.List();
            var result = _settingsMapper.ToAbsenceResources(absences.ToList());

            _logger.LogInformation("All absences retrieved successfully");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing get all absences query");
            throw;
        }
    }
}
