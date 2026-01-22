using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Presentation.DTOs.Schedules;

namespace Klacks.Api.Application.Handlers.WorkChanges;

public class PutCommandHandler : BaseHandler, IRequestHandler<PutCommand<WorkChangeResource>, WorkChangeResource?>
{
    private readonly IWorkChangeRepository _workChangeRepository;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IUnitOfWork _unitOfWork;

    public PutCommandHandler(
        IWorkChangeRepository workChangeRepository,
        ScheduleMapper scheduleMapper,
        IUnitOfWork unitOfWork,
        ILogger<PutCommandHandler> logger)
        : base(logger)
    {
        _workChangeRepository = workChangeRepository;
        _scheduleMapper = scheduleMapper;
        _unitOfWork = unitOfWork;
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

        _logger.LogInformation("WorkChange updated successfully: {Id}", request.Resource.Id);
        return updatedWorkChange != null ? _scheduleMapper.ToWorkChangeResource(updatedWorkChange) : null;
    }
}
