using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Queries;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Presentation.DTOs.Schedules;

namespace Klacks.Api.Application.Handlers.Breaks;

public class ListQueryHandler : BaseHandler, IRequestHandler<ListQuery<BreakResource>, IEnumerable<BreakResource>>
{
    private readonly IBreakRepository _breakRepository;
    private readonly ScheduleMapper _scheduleMapper;

    public ListQueryHandler(
        IBreakRepository breakRepository,
        ScheduleMapper scheduleMapper,
        ILogger<ListQueryHandler> logger)
        : base(logger)
    {
        _breakRepository = breakRepository;
        _scheduleMapper = scheduleMapper;
    }

    public async Task<IEnumerable<BreakResource>> Handle(ListQuery<BreakResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var entities = await _breakRepository.List();
            return entities.Select(_scheduleMapper.ToBreakResource);
        }, "ListBreaks");
    }
}
