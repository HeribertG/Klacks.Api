// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Shifts;

public class PutCommandHandler : BaseHandler, IRequestHandler<PutCommand<ShiftResource>, ShiftResource?>
{
    private readonly IShiftRepository _shiftRepository;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IUnitOfWork _unitOfWork;

    public PutCommandHandler(
        IShiftRepository shiftRepository,
        ScheduleMapper scheduleMapper,
        IUnitOfWork unitOfWork,
        ILogger<PutCommandHandler> logger)
        : base(logger)
    {
        _shiftRepository = shiftRepository;
        _scheduleMapper = scheduleMapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<ShiftResource?> Handle(PutCommand<ShiftResource> request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating shift: Id={ShiftId}, Name={Name}, MacroId={MacroId}, ClientId={ClientId}, GroupCount={GroupCount}",
            request.Resource.Id, request.Resource.Name, request.Resource.MacroId, request.Resource.ClientId, request.Resource.Groups?.Count ?? 0);

        var shift = _scheduleMapper.ToShiftEntity(request.Resource);

        var resultShift = await _shiftRepository.PutWithSealedOrderHandling(shift);

        if (resultShift == null)
        {
            _logger.LogWarning("Shift not found for update: Id={ShiftId}", request.Resource.Id);
            return null;
        }

        await _unitOfWork.CompleteAsync();

        _logger.LogInformation("Shift updated successfully: Id={ShiftId}, Name={Name}, Status={Status}",
            resultShift.Id, resultShift.Name, resultShift.Status);

        return _scheduleMapper.ToShiftResource(resultShift);
    }
}
