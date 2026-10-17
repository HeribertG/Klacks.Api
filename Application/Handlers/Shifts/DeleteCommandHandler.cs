// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Handler for soft-deleting a Shift. Returns the deleted shift's resource, or null when not found.
/// Only a draft order (status OriginalOrder) can be deleted; a sealed order, a plannable shift and a cut piece are
/// refused with ShiftLockedException (409), because deleting one would orphan the rest of its order family. The one
/// exception is an administrator deleting a sealed order: the whole family goes with it, but only while no work, scenario
/// clone or container reference exists anywhere in the family (SealedOrderFamilyDeletion decides; the administrator role
/// is read from the current HTTP principal, so a call outside a web request never qualifies).
/// Caller-side guards (shift has cuts / active works) live in the delete_shift skill. A shift a group-restricted caller
/// may not see is answered like a missing one (null) and not touched.
/// </summary>
/// <param name="request">Contains the id of the shift to delete</param>

using Klacks.Api.Application.Commands;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Exceptions;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Services.Shifts;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Shifts;

public class DeleteCommandHandler : BaseHandler, IRequestHandler<DeleteCommand<ShiftResource>, ShiftResource?>
{
    private readonly IShiftRepository _shiftRepository;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ISealedOrderFamilyDeletion _familyDeletion;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IShiftVisibilityGuard _shiftVisibilityGuard;

    public DeleteCommandHandler(
        IShiftRepository shiftRepository,
        ScheduleMapper scheduleMapper,
        IUnitOfWork unitOfWork,
        ISealedOrderFamilyDeletion familyDeletion,
        IHttpContextAccessor httpContextAccessor,
        IShiftVisibilityGuard shiftVisibilityGuard,
        ILogger<DeleteCommandHandler> logger)
        : base(logger)
    {
        _shiftVisibilityGuard = shiftVisibilityGuard;
        _shiftRepository = shiftRepository;
        _scheduleMapper = scheduleMapper;
        _unitOfWork = unitOfWork;
        _familyDeletion = familyDeletion;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<ShiftResource?> Handle(DeleteCommand<ShiftResource> request, CancellationToken cancellationToken)
    {
        var existing = await _shiftRepository.Get(request.Id);
        if (existing == null || !await _shiftVisibilityGuard.IsVisibleAsync(existing, cancellationToken))
        {
            return null;
        }

        var resource = _scheduleMapper.ToShiftResource(existing);

        if (LockedShiftEditPolicy.IsDeletionLocked(existing))
        {
            if (!LockedShiftEditPolicy.IsOrderFamilyDeletable(existing) || !IsAdministrator())
            {
                _logger.LogWarning("Shift delete refused: Id={ShiftId}, Status={Status}", existing.Id, existing.Status);
                throw ShiftLockedException.ForDelete(existing);
            }

            await _familyDeletion.DeleteAsync(existing, cancellationToken);
            return resource;
        }

        await _shiftRepository.Delete(request.Id);
        await _unitOfWork.CompleteAsync();

        return resource;
    }

    private bool IsAdministrator() =>
        _httpContextAccessor.HttpContext?.User?.IsInRole(Roles.Admin) == true;
}
