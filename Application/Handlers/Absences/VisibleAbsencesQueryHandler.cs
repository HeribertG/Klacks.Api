using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Queries.Absences;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Absences;

public class VisibleAbsencesQueryHandler : IRequestHandler<VisibleAbsencesQuery, List<AbsenceResource>>
{
    private readonly IAbsenceRepository _absenceRepository;
    private readonly SettingsMapper _settingsMapper;
    private readonly ILogger<VisibleAbsencesQueryHandler> _logger;

    public VisibleAbsencesQueryHandler(
        IAbsenceRepository absenceRepository,
        SettingsMapper settingsMapper,
        ILogger<VisibleAbsencesQueryHandler> logger)
    {
        _absenceRepository = absenceRepository;
        _settingsMapper = settingsMapper;
        _logger = logger;
    }

    public async Task<List<AbsenceResource>> Handle(VisibleAbsencesQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing get visible absences query (hideInGantt = false)");

        var absences = await _absenceRepository.List();
        var visibleAbsences = absences.Where(a => !a.HideInGantt).ToList();
        var result = _settingsMapper.ToAbsenceResources(visibleAbsences);

        _logger.LogInformation("Visible absences retrieved successfully: {Count} items", result.Count);
        return result;
    }
}
