using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Presentation.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Works;

public class PostCommandHandler : BaseHandler, IRequestHandler<PostCommand<WorkResource>, WorkResource?>
{
    private readonly IWorkRepository _workRepository;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IUnitOfWork _unitOfWork;

    public PostCommandHandler(
        IWorkRepository workRepository,
        ScheduleMapper scheduleMapper,
        IUnitOfWork unitOfWork,
        ILogger<PostCommandHandler> logger)
        : base(logger)
    {
        _workRepository = workRepository;
        _scheduleMapper = scheduleMapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<WorkResource?> Handle(PostCommand<WorkResource> request, CancellationToken cancellationToken)
    {
        var work = _scheduleMapper.ToWorkEntity(request.Resource);
        await _workRepository.Add(work);
        await _unitOfWork.CompleteAsync();
        return _scheduleMapper.ToWorkResource(work);
    }
}
