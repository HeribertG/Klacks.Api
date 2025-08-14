using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Presentation.DTOs.Settings;
using MediatR;

namespace Klacks.Api.Application.Handlers.States;

public class ListQueryHandler : IRequestHandler<ListQuery<StateResource>, IEnumerable<StateResource>>
{
    private readonly IStateRepository _stateRepository;
    private readonly IMapper _mapper;

    public ListQueryHandler(IStateRepository stateRepository, IMapper mapper)
    {
        _stateRepository = stateRepository;
        _mapper = mapper;
    }

    public async Task<IEnumerable<StateResource>> Handle(ListQuery<StateResource> request, CancellationToken cancellationToken)
    {
        var states = await _stateRepository.List();
        return _mapper.Map<IEnumerable<StateResource>>(states);
    }
}
