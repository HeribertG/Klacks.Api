using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Presentation.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.BreakContexts;

public class ListQueryHandler : IRequestHandler<ListQuery<BreakContextResource>, IEnumerable<BreakContextResource>>
{
    private readonly IBreakContextRepository _breakContextRepository;
    private readonly SettingsMapper _settingsMapper;

    public ListQueryHandler(IBreakContextRepository breakContextRepository, SettingsMapper settingsMapper)
    {
        _breakContextRepository = breakContextRepository;
        _settingsMapper = settingsMapper;
    }

    public async Task<IEnumerable<BreakContextResource>> Handle(ListQuery<BreakContextResource> request, CancellationToken cancellationToken)
    {
        var breakContexts = await _breakContextRepository.List();
        return _settingsMapper.ToBreakContextResources(breakContexts.ToList());
    }
}
