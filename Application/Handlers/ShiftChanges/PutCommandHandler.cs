using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Presentation.DTOs.Schedules;

namespace Klacks.Api.Application.Handlers.ShiftChanges;

public class PutCommandHandler : BaseHandler, IRequestHandler<PutCommand<ShiftChangeResource>, ShiftChangeResource?>
{
    private readonly IShiftChangeRepository _shiftChangeRepository;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IUnitOfWork _unitOfWork;

    public PutCommandHandler(
        IShiftChangeRepository shiftChangeRepository,
        ScheduleMapper scheduleMapper,
        IUnitOfWork unitOfWork,
        ILogger<PutCommandHandler> logger)
        : base(logger)
    {
        _shiftChangeRepository = shiftChangeRepository;
        _scheduleMapper = scheduleMapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<ShiftChangeResource?> Handle(PutCommand<ShiftChangeResource> request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating ShiftChange with ID: {Id}", request.Resource.Id);

        var existingShiftChange = await _shiftChangeRepository.Get(request.Resource.Id);
        if (existingShiftChange == null)
        {
            _logger.LogWarning("ShiftChange not found: {Id}", request.Resource.Id);
            return null;
        }

        var shiftChange = _scheduleMapper.ToShiftChangeEntity(request.Resource);
        var updatedShiftChange = await _shiftChangeRepository.Put(shiftChange);
        await _unitOfWork.CompleteAsync();

        _logger.LogInformation("ShiftChange updated successfully: {Id}", request.Resource.Id);
        return updatedShiftChange != null ? _scheduleMapper.ToShiftChangeResource(updatedShiftChange) : null;
    }
}
