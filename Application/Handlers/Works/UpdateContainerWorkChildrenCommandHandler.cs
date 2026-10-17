// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Handler for updating all children (sub-works, sub-breaks, and work changes) of a container work using the Savebar pattern.
/// Delegates business logic to IContainerWorkChildrenManager; handles lock verification, mapping, persistence, and notifications.
/// A container whose owner, any client of its sub-works or any replacement client of its work changes is outside the
/// caller's group visibility is refused exactly like a container that does not exist; nothing is written. A new sub-work or
/// a sub-work whose shift changes against the stored one must be on a shift the caller may book, otherwise the save is
/// refused exactly like a save with a shift that does not exist; unchanged sub-works on hidden shifts stay saveable.
/// </summary>
/// <param name="rowAccessGuard">Decides whether the calling user may see the owning client and the stored row's scenario</param>
/// <param name="shiftVisibilityGuard">Decides which shifts of new or changed sub-works the calling user may book</param>
using Klacks.Api.Application.Commands.Works;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Services.Shifts;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Works;

public class UpdateContainerWorkChildrenCommandHandler : BaseHandler, IRequestHandler<UpdateContainerWorkChildrenCommand, ContainerWorkChildrenResource>
{
    private const string WorkNotFoundMessageFormat = "Work with ID {0} not found.";

    private readonly IContainerWorkChildrenReadRepository _childrenReadRepository;
    private readonly IScheduleRowAccessGuard _rowAccessGuard;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWorkNotificationFacade _notificationFacade;
    private readonly IContainerLockRepository _lockRepository;
    private readonly IUserService _userService;
    private readonly IContainerWorkChildrenManager _childrenManager;
    private readonly IOvertimeCascadeService _overtimeCascadeService;
    private readonly IShiftVisibilityGuard _shiftVisibilityGuard;

    public UpdateContainerWorkChildrenCommandHandler(
        IContainerWorkChildrenReadRepository childrenReadRepository,
        IScheduleRowAccessGuard rowAccessGuard,
        ScheduleMapper scheduleMapper,
        IUnitOfWork unitOfWork,
        IWorkNotificationFacade notificationFacade,
        IContainerLockRepository lockRepository,
        IUserService userService,
        IContainerWorkChildrenManager childrenManager,
        IOvertimeCascadeService overtimeCascadeService,
        IShiftVisibilityGuard shiftVisibilityGuard,
        ILogger<UpdateContainerWorkChildrenCommandHandler> logger)
        : base(logger)
    {
        _childrenReadRepository = childrenReadRepository;
        _rowAccessGuard = rowAccessGuard;
        _scheduleMapper = scheduleMapper;
        _unitOfWork = unitOfWork;
        _notificationFacade = notificationFacade;
        _lockRepository = lockRepository;
        _userService = userService;
        _childrenManager = childrenManager;
        _overtimeCascadeService = overtimeCascadeService;
        _shiftVisibilityGuard = shiftVisibilityGuard;
    }

    public async Task<ContainerWorkChildrenResource> Handle(UpdateContainerWorkChildrenCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            if (request.Resource == null)
            {
                throw new ArgumentException("UpdateContainerWorkChildren request body is missing or could not be deserialized.");
            }

            await VerifyLockAsync(request.WorkId, cancellationToken);
            await EnsureContainerVisibleAsync(request.WorkId, request.Resource, cancellationToken);

            var updatedWorks = request.Resource.SubWorks.Select(_scheduleMapper.ToWorkEntity).ToList();
            var updatedBreaks = request.Resource.SubBreaks.Select(_scheduleMapper.ToBreakEntity).ToList();
            var updatedWorkChanges = request.Resource.SubWorkChanges.Select(_scheduleMapper.ToWorkChangeEntity).ToList();

            await EnsureSubWorkShiftsVisibleAsync(request.WorkId, updatedWorks, cancellationToken);

            var parentWork = await _childrenManager.UpdateChildrenAsync(
                request.WorkId,
                request.Resource.ParentStartBase,
                request.Resource.ParentEndBase,
                request.Resource.ParentStartTime,
                request.Resource.ParentEndTime,
                updatedWorks,
                updatedBreaks,
                updatedWorkChanges,
                cancellationToken);

            await _unitOfWork.CompleteAsync();

            // K3/K4 cascade: sub-work and parent WorkTime changes shift the prior-hours sums of the
            // client's later Works in the overtime basis period; anchors cover the parent and every
            // synced sub-work, deduplicated to one successor query per client and day.
            var cascadeAnchors = new List<Work>(updatedWorks);
            if (parentWork != null)
            {
                cascadeAnchors.Add(parentWork);
            }

            if (cascadeAnchors.Count > 0)
            {
                await _overtimeCascadeService.ReprocessSuccessorsAsync(cascadeAnchors);
            }

            await NotifyAffectedShiftsAsync(parentWork, cancellationToken);

            return await BuildResponseAsync(request.WorkId, parentWork, cancellationToken);
        }, nameof(Handle), new { request.WorkId });
    }

    private async Task VerifyLockAsync(Guid workId, CancellationToken cancellationToken)
    {
        var userId = _userService.GetId() ?? Guid.Empty;
        var instanceId = _userService.GetInstanceId() ?? string.Empty;
        var holdsLock = await _lockRepository.IsHeldBy(ContainerLockResourceTypes.ContainerWork, workId, userId, instanceId, cancellationToken);
        if (!holdsLock)
        {
            throw new ContainerLockedException("Cannot save: container work is not locked by this session.");
        }
    }

    /// <summary>
    /// Sub-works are persisted with the client id the request carries, so every non-empty one is checked
    /// together with the container owner and the replacement clients in one query. Sub-breaks are not:
    /// the children manager overwrites their client with the container owner.
    /// </summary>
    private async Task EnsureContainerVisibleAsync(
        Guid workId, UpdateContainerWorkChildrenResource resource, CancellationToken cancellationToken)
    {
        var parentWork = await _childrenReadRepository.GetParentWorkNoTracking(workId, cancellationToken);
        if (parentWork == null)
        {
            throw new KeyNotFoundException(string.Format(WorkNotFoundMessageFormat, workId));
        }

        var clientIds = resource.SubWorkChanges
            .Where(wc => wc.ReplaceClientId.HasValue)
            .Select(wc => wc.ReplaceClientId!.Value)
            .Concat(resource.SubWorks.Select(w => w.ClientId).Where(id => id != Guid.Empty))
            .Append(parentWork.ClientId)
            .Distinct()
            .ToList();
        if (!await _rowAccessGuard.AreAllVisibleAsync(clientIds, parentWork.AnalyseToken, cancellationToken))
        {
            throw new KeyNotFoundException(string.Format(WorkNotFoundMessageFormat, workId));
        }
    }

    /// <summary>
    /// One query decides which requested sub-work shifts the caller may book; only when some may not are the stored
    /// sub-works read, so that a hidden shift is refused only on a new sub-work or a changed shift.
    /// </summary>
    private async Task EnsureSubWorkShiftsVisibleAsync(
        Guid workId, IReadOnlyCollection<Work> updatedWorks, CancellationToken cancellationToken)
    {
        var requestedShiftIds = updatedWorks
            .Select(w => w.ShiftId)
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();
        if (requestedShiftIds.Count == 0)
        {
            return;
        }

        var visibleShiftIds = await _shiftVisibilityGuard.FilterVisibleAsync(requestedShiftIds, cancellationToken);
        if (visibleShiftIds.Count == requestedShiftIds.Count)
        {
            return;
        }

        var storedShiftIds = await _childrenReadRepository.GetChildWorkShiftIds(workId, cancellationToken);
        foreach (var subWork in updatedWorks)
        {
            if (subWork.ShiftId == Guid.Empty || visibleShiftIds.Contains(subWork.ShiftId))
            {
                continue;
            }

            var unchanged = subWork.Id != Guid.Empty
                && storedShiftIds.TryGetValue(subWork.Id, out var storedShiftId)
                && storedShiftId == subWork.ShiftId;
            if (!unchanged)
            {
                throw new KeyNotFoundException(string.Format(ShiftVisibilityMessages.ShiftNotFound, subWork.ShiftId));
            }
        }
    }

    private async Task NotifyAffectedShiftsAsync(Work? parentWork, CancellationToken cancellationToken)
    {
        if (parentWork == null) return;

        var affectedShifts = new HashSet<(Guid ShiftId, DateOnly Date)>
        {
            (parentWork.ShiftId, parentWork.CurrentDate)
        };

        var connectionId = _notificationFacade.GetConnectionId();
        await _notificationFacade.NotifyShiftStatsAsync(affectedShifts, connectionId, parentWork.AnalyseToken, cancellationToken);
    }

    private async Task<ContainerWorkChildrenResource> BuildResponseAsync(
        Guid workId, Work? parentWork, CancellationToken cancellationToken)
    {
        var reloadedWorks = await _childrenReadRepository.GetChildWorksWithShiftClient(workId, cancellationToken);

        var reloadedBreaks = await _childrenReadRepository.GetChildBreaksWithAbsence(workId, cancellationToken);

        var reloadedSubWorkIds = reloadedWorks.Select(w => w.Id).ToList();

        var reloadedWorkChanges = await _childrenReadRepository.GetWorkChangesForWorks(reloadedSubWorkIds, cancellationToken);

        return new ContainerWorkChildrenResource
        {
            SubWorks = reloadedWorks.Select(_scheduleMapper.ToWorkResource).ToList(),
            SubBreaks = reloadedBreaks.Select(_scheduleMapper.ToBreakResource).ToList(),
            SubWorkChanges = reloadedWorkChanges.Select(_scheduleMapper.ToWorkChangeResource).ToList(),
            ParentStartBase = parentWork?.StartBase,
            ParentEndBase = parentWork?.EndBase,
            ParentTransportMode = parentWork?.TransportMode.HasValue == true ? (int)parentWork.TransportMode.Value : null,
            ParentWorkTime = parentWork?.WorkTime ?? 0m
        };
    }
}
