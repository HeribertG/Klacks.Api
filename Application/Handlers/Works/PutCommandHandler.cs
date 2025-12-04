using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Presentation.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Works;

public class PutCommandHandler : BaseHandler, IRequestHandler<PutCommand<WorkResource>, WorkResource?>
{
    private readonly IWorkRepository _workRepository;
    private readonly ScheduleMapper _scheduleMapper;

    public PutCommandHandler(
        IWorkRepository workRepository, ScheduleMapper scheduleMapper,
        ILogger<PutCommandHandler> logger)
        : base(logger)
    {
        _workRepository = workRepository;
        _scheduleMapper = scheduleMapper;
    }

    public async Task<WorkResource?> Handle(PutCommand<WorkResource> request, CancellationToken cancellationToken)
    {
        var work = _scheduleMapper.ToWorkEntity(request.Resource);
        var updatedWork = await _workRepository.Put(work);
        return updatedWork != null ? _scheduleMapper.ToWorkResource(updatedWork) : null;
    }
}
