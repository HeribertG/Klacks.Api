using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Works;
using Klacks.Api.Domain.Models.Filters;
using Klacks.Api.Presentation.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Works;

public class ListQueryHandler : IRequestHandler<ListQuery, IEnumerable<ClientWorkResource>>
{
    private readonly IClientWorkRepository _clientWorkRepository;
    private readonly IMapper _mapper;

    public ListQueryHandler(
        IClientWorkRepository clientWorkRepository, 
        IMapper mapper)
    {
        _clientWorkRepository = clientWorkRepository;
        _mapper = mapper;
    }

    public async Task<IEnumerable<ClientWorkResource>> Handle(ListQuery request, CancellationToken cancellationToken)
    {
        var workFilter = _mapper.Map<WorkFilter>(request.Filter);
        
        var clients = await _clientWorkRepository.WorkList(workFilter);
        return _mapper.Map<IEnumerable<ClientWorkResource>>(clients);
    }
}
