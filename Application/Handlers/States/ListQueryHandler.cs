using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Presentation.DTOs.Settings;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.States;

public class ListQueryHandler : IRequestHandler<ListQuery<StateResource>, IEnumerable<StateResource>>
{
    private readonly IStateRepository _stateRepository;
    private readonly SettingsMapper _settingsMapper;

    public ListQueryHandler(IStateRepository stateRepository, SettingsMapper settingsMapper)
    {
        _stateRepository = stateRepository;
        _settingsMapper = settingsMapper;
    }

    public async Task<IEnumerable<StateResource>> Handle(ListQuery<StateResource> request, CancellationToken cancellationToken)
    {
        var states = await _stateRepository.List();
        return _settingsMapper.ToStateResources(states.ToList());
    }
}
