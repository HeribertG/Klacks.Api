using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Queries;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.WorkChanges;

public class GetQueryHandler : BaseHandler, IRequestHandler<GetQuery<WorkChangeResource>, WorkChangeResource>
{
    private readonly IWorkChangeRepository _workChangeRepository;
    private readonly ScheduleMapper _scheduleMapper;

    public GetQueryHandler(
        IWorkChangeRepository workChangeRepository,
        ScheduleMapper scheduleMapper,
        ILogger<GetQueryHandler> logger)
        : base(logger)
    {
        _workChangeRepository = workChangeRepository;
        _scheduleMapper = scheduleMapper;
    }

    public async Task<WorkChangeResource> Handle(GetQuery<WorkChangeResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var workChange = await _workChangeRepository.Get(request.Id);

            if (workChange == null)
            {
                throw new KeyNotFoundException($"WorkChange with ID {request.Id} not found");
            }

            return _scheduleMapper.ToWorkChangeResource(workChange);
        }, nameof(Handle), new { request.Id });
    }
}
