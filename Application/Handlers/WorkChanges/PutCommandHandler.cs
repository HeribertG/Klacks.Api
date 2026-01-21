using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Infrastructure.Services;
using Klacks.Api.Presentation.DTOs.Schedules;

namespace Klacks.Api.Application.Handlers.WorkChanges;

public class PutCommandHandler : BaseHandler, IRequestHandler<PutCommand<WorkChangeResource>, WorkChangeResource?>
{
    private readonly IWorkChangeRepository _workChangeRepository;
    private readonly IWorkRepository _workRepository;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly PeriodHoursBackgroundService _periodHoursBackgroundService;

    public PutCommandHandler(
        IWorkChangeRepository workChangeRepository,
        IWorkRepository workRepository,
        ScheduleMapper scheduleMapper,
        IUnitOfWork unitOfWork,
        PeriodHoursBackgroundService periodHoursBackgroundService,
        ILogger<PutCommandHandler> logger)
        : base(logger)
    {
        _workChangeRepository = workChangeRepository;
        _workRepository = workRepository;
        _scheduleMapper = scheduleMapper;
        _unitOfWork = unitOfWork;
        _periodHoursBackgroundService = periodHoursBackgroundService;
    }

    public async Task<WorkChangeResource?> Handle(PutCommand<WorkChangeResource> request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating WorkChange with ID: {Id}", request.Resource.Id);

        var existingWorkChange = await _workChangeRepository.Get(request.Resource.Id);
        if (existingWorkChange == null)
        {
            _logger.LogWarning("WorkChange not found: {Id}", request.Resource.Id);
            return null;
        }

        var workChange = _scheduleMapper.ToWorkChangeEntity(request.Resource);
        var updatedWorkChange = await _workChangeRepository.Put(workChange);
        await _unitOfWork.CompleteAsync();

        var work = await _workRepository.Get(workChange.WorkId);
        if (work != null)
        {
            _periodHoursBackgroundService.QueueRecalculation(
                work.ClientId,
                DateOnly.FromDateTime(work.CurrentDate));
        }

        _logger.LogInformation("WorkChange updated successfully: {Id}", request.Resource.Id);
        return updatedWorkChange != null ? _scheduleMapper.ToWorkChangeResource(updatedWorkChange) : null;
    }
}
