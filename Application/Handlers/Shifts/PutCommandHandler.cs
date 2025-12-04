using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Presentation.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Shifts;

public class PutCommandHandler : BaseHandler, IRequestHandler<PutCommand<ShiftResource>, ShiftResource?>
{
    private readonly IShiftRepository _shiftRepository;
    private readonly ICreateShiftFromOrderService _createShiftFromOrderService;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IUnitOfWork _unitOfWork;

    public PutCommandHandler(
        IShiftRepository shiftRepository,
        ICreateShiftFromOrderService createShiftFromOrderService,
        ScheduleMapper scheduleMapper,
        IUnitOfWork unitOfWork,
        ILogger<PutCommandHandler> logger)
        : base(logger)
    {
        _shiftRepository = shiftRepository;
        _createShiftFromOrderService = createShiftFromOrderService;
        _scheduleMapper = scheduleMapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<ShiftResource?> Handle(PutCommand<ShiftResource> request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating shift: Id={ShiftId}, Name={Name}, MacroId={MacroId}, ClientId={ClientId}, GroupCount={GroupCount}",
            request.Resource.Id, request.Resource.Name, request.Resource.MacroId, request.Resource.ClientId, request.Resource.Groups?.Count ?? 0);

        var shift = _scheduleMapper.ToShiftEntity(request.Resource);

        var updatedShift = await _shiftRepository.Put(shift);

        if (updatedShift == null)
        {
            _logger.LogWarning("Shift not found for update: Id={ShiftId}", request.Resource.Id);
            return null;
        }

        await _unitOfWork.CompleteAsync();

        var resultShift = await _createShiftFromOrderService.CreateFromSealedOrder(updatedShift);

        if (resultShift.Id != updatedShift.Id)
        {
            await _unitOfWork.CompleteAsync();
        }

        _logger.LogInformation("Shift updated successfully: Id={ShiftId}, Name={Name}, Status={Status}",
            resultShift.Id, resultShift.Name, resultShift.Status);

        return _scheduleMapper.ToShiftResource(resultShift);
    }
}
