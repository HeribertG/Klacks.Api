using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Queries;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Presentation.DTOs.Schedules;

namespace Klacks.Api.Application.Handlers.Breaks;

public class GetQueryHandler : BaseHandler, IRequestHandler<GetQuery<BreakResource>, BreakResource>
{
    private readonly IBreakRepository _breakRepository;
    private readonly ScheduleMapper _scheduleMapper;

    public GetQueryHandler(
        IBreakRepository breakRepository,
        ScheduleMapper scheduleMapper,
        ILogger<GetQueryHandler> logger)
        : base(logger)
    {
        _breakRepository = breakRepository;
        _scheduleMapper = scheduleMapper;
    }

    public async Task<BreakResource> Handle(GetQuery<BreakResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var entity = await _breakRepository.Get(request.Id);

            if (entity == null)
            {
                throw new KeyNotFoundException($"Break with ID {request.Id} not found");
            }

            return _scheduleMapper.ToBreakResource(entity);
        }, "GetBreak", new { request.Id });
    }
}
