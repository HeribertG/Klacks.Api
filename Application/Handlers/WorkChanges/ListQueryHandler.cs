using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Queries;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.Api.Application.Handlers.WorkChanges;

public class ListQueryHandler : BaseHandler, IRequestHandler<ListQuery<WorkChangeResource>, IEnumerable<WorkChangeResource>>
{
    private readonly IWorkChangeRepository _workChangeRepository;
    private readonly ScheduleMapper _scheduleMapper;

    public ListQueryHandler(
        IWorkChangeRepository workChangeRepository,
        ScheduleMapper scheduleMapper,
        ILogger<ListQueryHandler> logger)
        : base(logger)
    {
        _workChangeRepository = workChangeRepository;
        _scheduleMapper = scheduleMapper;
    }

    public async Task<IEnumerable<WorkChangeResource>> Handle(ListQuery<WorkChangeResource> request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting all WorkChanges");

        var workChanges = await _workChangeRepository.List();

        _logger.LogInformation("Successfully retrieved {Count} WorkChanges", workChanges.Count);
        return _scheduleMapper.ToWorkChangeResourceList(workChanges);
    }
}
