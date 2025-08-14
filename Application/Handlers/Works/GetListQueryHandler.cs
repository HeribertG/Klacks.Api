using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Works;
using Klacks.Api.Presentation.DTOs.Schedules;
using MediatR;

namespace Klacks.Api.Application.Handlers.Works;

public class GetListQueryHandler : IRequestHandler<ListQuery, IEnumerable<ClientWorkResource>>
{
    private readonly IClientRepository _clientRepository;
    private readonly IMapper _mapper;

    public GetListQueryHandler(IClientRepository clientRepository, IMapper mapper)
    {
        _clientRepository = clientRepository;
        _mapper = mapper;
    }

    public async Task<IEnumerable<ClientWorkResource>> Handle(ListQuery request, CancellationToken cancellationToken)
    {
        var clients = await _clientRepository.WorkList(request.Filter);
        return _mapper.Map<IEnumerable<ClientWorkResource>>(clients);
    }
}
