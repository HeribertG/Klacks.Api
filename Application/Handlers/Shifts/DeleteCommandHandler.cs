// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for soft-deleting a Shift. Returns the deleted shift's resource, or null when not found.
/// Caller-side guards (shift has cuts / active works) live in the delete_shift skill.
/// </summary>
/// <param name="request">Contains the id of the shift to delete</param>

using Klacks.Api.Application.Commands;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Shifts;

public class DeleteCommandHandler : BaseHandler, IRequestHandler<DeleteCommand<ShiftResource>, ShiftResource?>
{
    private readonly IShiftRepository _shiftRepository;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteCommandHandler(
        IShiftRepository shiftRepository,
        ScheduleMapper scheduleMapper,
        IUnitOfWork unitOfWork,
        ILogger<DeleteCommandHandler> logger)
        : base(logger)
    {
        _shiftRepository = shiftRepository;
        _scheduleMapper = scheduleMapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<ShiftResource?> Handle(DeleteCommand<ShiftResource> request, CancellationToken cancellationToken)
    {
        var existing = await _shiftRepository.Get(request.Id);
        if (existing == null)
        {
            return null;
        }

        var resource = _scheduleMapper.ToShiftResource(existing);

        await _shiftRepository.Delete(request.Id);
        await _unitOfWork.CompleteAsync();

        return resource;
    }
}
