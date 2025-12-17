using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Presentation.DTOs.Schedules;

namespace Klacks.Api.Application.Handlers.WorkChanges;

public class PostCommandHandler : BaseHandler, IRequestHandler<PostCommand<WorkChangeResource>, WorkChangeResource?>
{
    private readonly IWorkChangeRepository _workChangeRepository;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IUnitOfWork _unitOfWork;

    public PostCommandHandler(
        IWorkChangeRepository workChangeRepository,
        ScheduleMapper scheduleMapper,
        IUnitOfWork unitOfWork,
        ILogger<PostCommandHandler> logger)
        : base(logger)
    {
        _workChangeRepository = workChangeRepository;
        _scheduleMapper = scheduleMapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<WorkChangeResource?> Handle(PostCommand<WorkChangeResource> request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating new WorkChange");

        var workChange = _scheduleMapper.ToWorkChangeEntity(request.Resource);
        await _workChangeRepository.Add(workChange);
        await _unitOfWork.CompleteAsync();

        _logger.LogInformation("WorkChange created successfully with ID: {Id}", workChange.Id);
        return _scheduleMapper.ToWorkChangeResource(workChange);
    }
}
