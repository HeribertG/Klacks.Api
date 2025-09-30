using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Works;
using Klacks.Api.Domain.Models.Filters;
using Klacks.Api.Presentation.DTOs.Schedules;
using MediatR;

namespace Klacks.Api.Application.Handlers.Works;

public class ListQueryHandler : IRequestHandler<ListQuery, IEnumerable<ClientWorkResource>>
{
    private readonly IClientRepository _clientRepository;
    private readonly IMapper _mapper;

    public ListQueryHandler(
        IClientRepository clientRepository, 
        IMapper mapper)
    {
        _clientRepository = clientRepository;
        _mapper = mapper;
    }

    public async Task<IEnumerable<ClientWorkResource>> Handle(ListQuery request, CancellationToken cancellationToken)
    {
        var workFilter = _mapper.Map<WorkFilter>(request.Filter);
        var pagination = new PaginationParams { PageIndex = 0, PageSize = 1000 }; // Default für Liste
        
        var clients = await _clientRepository.WorkList(workFilter, pagination);
        return _mapper.Map<IEnumerable<ClientWorkResource>>(clients);
    }
}
