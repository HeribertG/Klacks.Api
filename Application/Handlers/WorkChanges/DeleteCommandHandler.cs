using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Presentation.DTOs.Schedules;

namespace Klacks.Api.Application.Handlers.WorkChanges;

public class DeleteCommandHandler : BaseHandler, IRequestHandler<DeleteCommand<WorkChangeResource>, WorkChangeResource?>
{
    private readonly IWorkChangeRepository _workChangeRepository;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteCommandHandler(
        IWorkChangeRepository workChangeRepository,
        ScheduleMapper scheduleMapper,
        IUnitOfWork unitOfWork,
        ILogger<DeleteCommandHandler> logger)
        : base(logger)
    {
        _workChangeRepository = workChangeRepository;
        _scheduleMapper = scheduleMapper;
        _unitOfWork = unitOfWork;
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

        var workChangeResource = _scheduleMapper.ToWorkChangeResource(existingWorkChange);

        await _workChangeRepository.Delete(request.Id);
        await _unitOfWork.CompleteAsync();

        _logger.LogInformation("WorkChange deleted successfully: {Id}", request.Id);
        return workChangeResource;
    }
}
