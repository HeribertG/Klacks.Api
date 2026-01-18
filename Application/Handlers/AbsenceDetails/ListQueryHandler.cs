using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Presentation.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.AbsenceDetails;

public class ListQueryHandler : IRequestHandler<ListQuery<AbsenceDetailResource>, IEnumerable<AbsenceDetailResource>>
{
    private readonly IAbsenceDetailRepository _absenceDetailRepository;
    private readonly SettingsMapper _settingsMapper;

    public ListQueryHandler(IAbsenceDetailRepository absenceDetailRepository, SettingsMapper settingsMapper)
    {
        _absenceDetailRepository = absenceDetailRepository;
        _settingsMapper = settingsMapper;
    }

    public async Task<IEnumerable<AbsenceDetailResource>> Handle(ListQuery<AbsenceDetailResource> request, CancellationToken cancellationToken)
    {
        var absenceDetails = await _absenceDetailRepository.List();
        return _settingsMapper.ToAbsenceDetailResources(absenceDetails.ToList());
    }
}
