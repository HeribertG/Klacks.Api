using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Works;
using Klacks.Api.Domain.Models.Filters;
using Klacks.Api.Presentation.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Works;

public class ListQueryHandler : IRequestHandler<ListQuery, IEnumerable<ClientWorkResource>>
{
    private readonly IClientWorkRepository _clientWorkRepository;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly FilterMapper _filterMapper;
    private readonly ClientMapper _clientMapper;

    public ListQueryHandler(
        IClientWorkRepository clientWorkRepository,
        ScheduleMapper scheduleMapper,
        FilterMapper filterMapper,
        ClientMapper clientMapper)
    {
        _clientWorkRepository = clientWorkRepository;
        _scheduleMapper = scheduleMapper;
        _filterMapper = filterMapper;
        _clientMapper = clientMapper;
    }

    public async Task<IEnumerable<ClientWorkResource>> Handle(ListQuery request, CancellationToken cancellationToken)
    {
        var workFilter = _filterMapper.ToWorkFilter(request.Filter);

        var clients = await _clientWorkRepository.WorkList(workFilter);
        return clients.Select(c => _clientMapper.ToWorkResource(c)).ToList();
    }
}
