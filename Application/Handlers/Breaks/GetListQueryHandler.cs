using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Breaks;
using Klacks.Api.Presentation.DTOs.Schedules;
using MediatR;

namespace Klacks.Api.Application.Handlers.Breaks;

public class GetListQueryHandler(IClientRepository clientRepository, IMapper mapper) : IRequestHandler<ListQuery, IEnumerable<ClientBreakResource>>
{
    private readonly IClientRepository _clientRepository = clientRepository;
    private readonly IMapper _mapper = mapper;

    public async Task<IEnumerable<ClientBreakResource>> Handle(ListQuery request, CancellationToken cancellationToken)
    {
        var clients = await _clientRepository.BreakList(request.Filter);
        return _mapper.Map<IEnumerable<ClientBreakResource>>(clients);
    }
}
