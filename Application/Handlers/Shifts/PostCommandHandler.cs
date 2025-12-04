using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Presentation.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Shifts;

public class PostCommandHandler : BaseHandler, IRequestHandler<PostCommand<ShiftResource>, ShiftResource?>
{
    private readonly IShiftRepository _shiftRepository;
    private readonly ICreateShiftFromOrderService _createShiftFromOrderService;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IUnitOfWork _unitOfWork;

    public PostCommandHandler(
        IShiftRepository shiftRepository,
        ICreateShiftFromOrderService createShiftFromOrderService,
        ScheduleMapper scheduleMapper,
        IUnitOfWork unitOfWork,
        ILogger<PostCommandHandler> logger)
        : base(logger)
    {
        _shiftRepository = shiftRepository;
        _createShiftFromOrderService = createShiftFromOrderService;
        _scheduleMapper = scheduleMapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<ShiftResource?> Handle(PostCommand<ShiftResource> request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating new shift: Name={Name}, MacroId={MacroId}, ClientId={ClientId}, GroupCount={GroupCount}",
            request.Resource.Name, request.Resource.MacroId, request.Resource.ClientId, request.Resource.Groups?.Count ?? 0);

        var shift = _scheduleMapper.ToShiftEntity(request.Resource);

        if (shift.Status == ShiftStatus.SealedOrder)
        {
            _logger.LogInformation("Shift has SealedOrder status, will create OriginalShift copy after save: Name={Name}", shift.Name);
        }

        await _shiftRepository.Add(shift);
        await _unitOfWork.CompleteAsync();

        Shift resultShift = shift;

        if (shift.Status == ShiftStatus.SealedOrder)
        {
            resultShift = await _createShiftFromOrderService.CreateFromSealedOrder(shift);
            await _unitOfWork.CompleteAsync();
        }

        _logger.LogInformation("Shift created successfully: Id={ShiftId}, Name={Name}, Status={Status}",
            resultShift.Id, resultShift.Name, resultShift.Status);

        return _scheduleMapper.ToShiftResource(resultShift);
    }
}
