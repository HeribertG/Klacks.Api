using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Infrastructure.Services;
using Klacks.Api.Presentation.DTOs.Schedules;

namespace Klacks.Api.Application.Handlers.WorkChanges;

public class DeleteCommandHandler : BaseHandler, IRequestHandler<DeleteCommand<WorkChangeResource>, WorkChangeResource?>
{
    private readonly IWorkChangeRepository _workChangeRepository;
    private readonly IWorkRepository _workRepository;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly PeriodHoursBackgroundService _periodHoursBackgroundService;

    public DeleteCommandHandler(
        IWorkChangeRepository workChangeRepository,
        IWorkRepository workRepository,
        ScheduleMapper scheduleMapper,
        IUnitOfWork unitOfWork,
        PeriodHoursBackgroundService periodHoursBackgroundService,
        ILogger<DeleteCommandHandler> logger)
        : base(logger)
    {
        _workChangeRepository = workChangeRepository;
        _workRepository = workRepository;
        _scheduleMapper = scheduleMapper;
        _unitOfWork = unitOfWork;
        _periodHoursBackgroundService = periodHoursBackgroundService;
    }

    public async Task<WorkChangeResource?> Handle(DeleteCommand<WorkChangeResource> request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting WorkChange with ID: {Id}", request.Id);

        var existingWorkChange = await _workChangeRepository.Get(request.Id);
        if (existingWorkChange == null)
        {
            _logger.LogWarning("WorkChange not found: {Id}", request.Id);
            return null;
        }

        var workId = existingWorkChange.WorkId;
        var workChangeResource = _scheduleMapper.ToWorkChangeResource(existingWorkChange);

        await _workChangeRepository.Delete(request.Id);
        await _unitOfWork.CompleteAsync();

        var work = await _workRepository.Get(workId);
        if (work != null)
        {
            _periodHoursBackgroundService.QueueRecalculation(
                work.ClientId,
                DateOnly.FromDateTime(work.CurrentDate));
        }

        _logger.LogInformation("WorkChange deleted successfully: {Id}", request.Id);
        return workChangeResource;
    }
}
