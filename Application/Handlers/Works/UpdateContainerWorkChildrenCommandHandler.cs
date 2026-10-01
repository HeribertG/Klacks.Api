// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Handler for updating all children (sub-works, sub-breaks, and work changes) of a container work using the Savebar pattern.
/// Delegates business logic to IContainerWorkChildrenManager; handles lock verification, mapping, persistence, and notifications.
/// A container whose owner, any client of its sub-works or any replacement client of its work changes is outside the
/// caller's group visibility is refused exactly like a container that does not exist; nothing is written.
/// </summary>
/// <param name="clientVisibilityGuard">Decides whether the calling user may write for every client the save touches</param>
using Klacks.Api.Application.Commands.Works;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Works;

public class UpdateContainerWorkChildrenCommandHandler : BaseHandler, IRequestHandler<UpdateContainerWorkChildrenCommand, ContainerWorkChildrenResource>
{
    private const string LockResourceType = "ContainerWork";
    private const string WorkNotFoundMessageFormat = "Work with ID {0} not found.";

    private readonly IContainerWorkChildrenReadRepository _childrenReadRepository;
    private readonly IClientVisibilityGuard _clientVisibilityGuard;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWorkNotificationFacade _notificationFacade;
    private readonly IContainerLockRepository _lockRepository;
    private readonly IUserService _userService;
    private readonly IContainerWorkChildrenManager _childrenManager;
    private readonly IOvertimeCascadeService _overtimeCascadeService;

    public UpdateContainerWorkChildrenCommandHandler(
        IContainerWorkChildrenReadRepository childrenReadRepository,
        IClientVisibilityGuard clientVisibilityGuard,
        ScheduleMapper scheduleMapper,
        IUnitOfWork unitOfWork,
        IWorkNotificationFacade notificationFacade,
        IContainerLockRepository lockRepository,
        IUserService userService,
        IContainerWorkChildrenManager childrenManager,
        IOvertimeCascadeService overtimeCascadeService,
        ILogger<UpdateContainerWorkChildrenCommandHandler> logger)
        : base(logger)
    {
        _childrenReadRepository = childrenReadRepository;
        _clientVisibilityGuard = clientVisibilityGuard;
        _scheduleMapper = scheduleMapper;
        _unitOfWork = unitOfWork;
        _notificationFacade = notificationFacade;
        _lockRepository = lockRepository;
        _userService = userService;
        _childrenManager = childrenManager;
        _overtimeCascadeService = overtimeCascadeService;
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
        var holdsLock = await _lockRepository.IsHeldBy(LockResourceType, workId, userId, instanceId, cancellationToken);
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
        if (!await _clientVisibilityGuard.AreAllVisibleAsync(clientIds, cancellationToken))
        {
            throw new KeyNotFoundException(string.Format(WorkNotFoundMessageFormat, workId));
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
