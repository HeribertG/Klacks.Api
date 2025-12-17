using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Presentation.DTOs.Schedules;

namespace Klacks.Api.Application.Handlers.ShiftChanges;

public class DeleteCommandHandler : BaseHandler, IRequestHandler<DeleteCommand<ShiftChangeResource>, ShiftChangeResource?>
{
    private readonly IShiftChangeRepository _shiftChangeRepository;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteCommandHandler(
        IShiftChangeRepository shiftChangeRepository,
        ScheduleMapper scheduleMapper,
        IUnitOfWork unitOfWork,
        ILogger<DeleteCommandHandler> logger)
        : base(logger)
    {
        _shiftChangeRepository = shiftChangeRepository;
        _scheduleMapper = scheduleMapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<ShiftChangeResource?> Handle(DeleteCommand<ShiftChangeResource> request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting ShiftChange with ID: {Id}", request.Id);

        var existingShiftChange = await _shiftChangeRepository.Get(request.Id);
        if (existingShiftChange == null)
        {
            _logger.LogWarning("ShiftChange not found: {Id}", request.Id);
            return null;
        }

        var shiftChangeResource = _scheduleMapper.ToShiftChangeResource(existingShiftChange);

        await _shiftChangeRepository.Delete(request.Id);
        await _unitOfWork.CompleteAsync();

        _logger.LogInformation("ShiftChange deleted successfully: {Id}", request.Id);
        return shiftChangeResource;
    }
}
