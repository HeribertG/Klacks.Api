// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Handler for updating all children (sub-works, sub-breaks, and work changes) of a container work using the Savebar pattern.
/// Delegates business logic to IContainerWorkChildrenManager; handles lock verification, mapping, persistence, and notifications.
/// A container whose owner, any client of its sub-works or any replacement client of its work changes is outside the
/// caller's group visibility is refused exactly like a container that does not exist; nothing is written. A new sub-work or
/// a sub-work whose shift changes against the stored one must be on a shift the caller may book, otherwise the save is
/// refused exactly like a save with a shift that does not exist; unchanged sub-works on hidden shifts stay saveable. Every
/// sub-work shift must also lie in the world of the container's stored Work (IWorkWriteGuard), or it reads like a missing one.
/// A new or changed replacement among the work changes of a sub-work passes IReplacementWriteGuard like a replacement
/// saved on its own, with the stored row as comparison; a row that is the same as the stored one is not checked again.
/// The children keep their server-owned state (seal state, scenario, clone link) from the stored rows and pass the lock level
/// and day-lock checks of the update and delete handlers, a child left out of the request included (IContainerChildrenWriteGuard).
/// The owners of the stored children and the persons of the stored work changes must be visible to the caller as well.
/// </summary>
/// <param name="rowAccessGuard">Decides whether the calling user may see the owning client and the stored row's scenario</param>
/// <param name="shiftVisibilityGuard">Decides which shifts of new or changed sub-works the calling user may book</param>
/// <param name="workWriteGuard">Refuses a sub-work shift outside the world of the container's stored Work</param>
/// <param name="replacementWriteGuard">Refuses a new or changed replacement without a valid person or with a blocking conflict for the substitute</param>
/// <param name="containerWorkChangeGuard">Refuses a new or changed correction on the container itself</param>
/// <param name="childrenWriteGuard">Keeps the server-owned state of the children and refuses writes the lock levels and sealed days forbid</param>
using Klacks.Api.Application.Commands.Works;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Services.Schedules;
using Klacks.Api.Application.Services.Shifts;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Services.Schedules;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Works;

public class UpdateContainerWorkChildrenCommandHandler : BaseHandler, IRequestHandler<UpdateContainerWorkChildrenCommand, ContainerWorkChildrenResource>
{
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
    private readonly IWorkWriteGuard _workWriteGuard;
    private readonly IReplacementWriteGuard _replacementWriteGuard;
    private readonly IContainerWorkChangeGuard _containerWorkChangeGuard;
    private readonly IContainerChildrenWriteGuard _childrenWriteGuard;

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
        IWorkWriteGuard workWriteGuard,
        IReplacementWriteGuard replacementWriteGuard,
        IContainerWorkChangeGuard containerWorkChangeGuard,
        IContainerChildrenWriteGuard childrenWriteGuard,
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
        _workWriteGuard = workWriteGuard;
        _replacementWriteGuard = replacementWriteGuard;
        _containerWorkChangeGuard = containerWorkChangeGuard;
        _childrenWriteGuard = childrenWriteGuard;
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
            var container = await ReadContainerAsync(request.WorkId, cancellationToken);
            var stored = await LoadStoredChildrenAsync(container, cancellationToken);
            await EnsureContainerVisibleAsync(container, stored, request.Resource, cancellationToken);

            var updatedWorks = request.Resource.SubWorks.Select(_scheduleMapper.ToWorkEntity).ToList();
            var updatedBreaks = request.Resource.SubBreaks.Select(_scheduleMapper.ToBreakEntity).ToList();
            var updatedWorkChanges = request.Resource.SubWorkChanges.Select(_scheduleMapper.ToWorkChangeEntity).ToList();

            await EnsureSubWorkShiftsVisibleAsync(request.WorkId, updatedWorks, cancellationToken);
            await _workWriteGuard.EnsureShiftsInWorkWorldAsync(
                updatedWorks.Select(w => (w.ShiftId, container.AnalyseToken)).Distinct().ToList(), cancellationToken);
            var workChangeParents = ResolveWorkChangeParents(container, request.Resource, updatedWorks, stored);
            await _childrenWriteGuard.PrepareAsync(container, stored, updatedWorks, updatedBreaks, updatedWorkChanges, cancellationToken);
            await EnsureReplacementsAllowedAsync(container, request.Resource, workChangeParents, stored, cancellationToken);

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

    private async Task<Work> ReadContainerAsync(Guid workId, CancellationToken cancellationToken)
    {
        var container = await _childrenReadRepository.GetParentWorkNoTracking(workId, cancellationToken);
        if (container == null)
        {
            throw new KeyNotFoundException(string.Format(ContainerLockMessages.WorkNotFound, workId));
        }

        return container;
    }

    private async Task<StoredContainerChildren> LoadStoredChildrenAsync(Work container, CancellationToken cancellationToken)
    {
        var works = (await _childrenReadRepository.GetChildWorksWithShiftClient(container.Id, cancellationToken))
            .Where(work => !work.IsDeleted)
            .ToList();
        var breaks = (await _childrenReadRepository.GetChildBreaksWithAbsence(container.Id, cancellationToken))
            .Where(entry => !entry.IsDeleted)
            .ToList();
        var workChanges = await _childrenReadRepository.GetWorkChangesForWorks(works.Select(work => work.Id).ToList(), cancellationToken);

        return new StoredContainerChildren(works, breaks, workChanges);
    }

    /// <summary>
    /// Sub-works are persisted with the client id the request carries, so every non-empty one is checked
    /// together with the container owner and the replacement clients in one query. The owners of the stored children and the
    /// persons of the stored work changes are checked with them, since the save rewrites or deletes those rows too.
    /// Sub-breaks are not: the children manager overwrites their client with the container owner.
    /// </summary>
    private async Task EnsureContainerVisibleAsync(
        Work container,
        StoredContainerChildren stored,
        UpdateContainerWorkChildrenResource resource,
        CancellationToken cancellationToken)
    {
        var clientIds = resource.SubWorkChanges
            .Where(wc => wc.ReplaceClientId.HasValue)
            .Select(wc => wc.ReplaceClientId!.Value)
            .Concat(resource.SubWorks.Select(w => w.ClientId).Where(id => id != Guid.Empty))
            .Concat(stored.Works.Select(w => w.ClientId))
            .Concat(stored.Breaks.Select(b => b.ClientId))
            .Concat(stored.WorkChanges.Where(wc => wc.ReplaceClientId.HasValue).Select(wc => wc.ReplaceClientId!.Value))
            .Append(container.ClientId)
            .Distinct()
            .ToList();
        if (!await _rowAccessGuard.AreAllVisibleAsync(clientIds, container.AnalyseToken, cancellationToken))
        {
            throw new KeyNotFoundException(string.Format(ContainerLockMessages.WorkNotFound, container.Id));
        }
    }

    /// <summary>
    /// A work change is saved with the Work id the request carries, and a stored change takes that id over, so every Work id
    /// has to be a sub-work of this container: one the request names or one that is stored under the container. Any other id
    /// (a Work of another container, the container itself, an empty id) is answered exactly like a container that does not
    /// exist, before anything is checked or written; otherwise a holder of this container's lock could hang a change on a
    /// Work past the visibility, lock and day-lock checks of that Work. The sub-works that serve as parents of the changes
    /// are returned by id.
    /// </summary>
    private static IReadOnlyDictionary<Guid, Work> ResolveWorkChangeParents(
        Work container,
        UpdateContainerWorkChildrenResource resource,
        IReadOnlyCollection<Work> updatedWorks,
        StoredContainerChildren stored)
    {
        var parents = stored.Works.ToDictionary(work => work.Id);
        foreach (var work in updatedWorks.Where(work => work.Id != Guid.Empty))
        {
            parents[work.Id] = work;
        }

        if (resource.SubWorkChanges.Any(change => !parents.ContainsKey(change.WorkId)))
        {
            throw new KeyNotFoundException(string.Format(ContainerLockMessages.WorkNotFound, container.Id));
        }

        return parents;
    }

    /// <summary>
    /// Every work change of the request that is new or differs from the stored row in what a replacement is made of (Work,
    /// type, person, times) passes the container guard (no correction on the container itself) and the replacement write
    /// guard against the work it is attached to. The replacement guard sees the
    /// scenario of the container and the stored row; it works on a copy, so what is saved stays as the request carries it.
    /// Unchanged rows are skipped, so a stored row the guard would refuse today does not block saving the rest of the
    /// container.
    /// </summary>
    private async Task EnsureReplacementsAllowedAsync(
        Work container,
        UpdateContainerWorkChildrenResource resource,
        IReadOnlyDictionary<Guid, Work> workChangeParents,
        StoredContainerChildren stored,
        CancellationToken cancellationToken)
    {
        var storedChanges = stored.WorkChanges.ToDictionary(change => change.Id);

        foreach (var requested in resource.SubWorkChanges)
        {
            var candidate = _scheduleMapper.ToWorkChangeEntity(requested);
            storedChanges.TryGetValue(candidate.Id, out var storedChange);
            if (storedChange != null && WorkChangeContent.AreSame(storedChange, candidate))
            {
                continue;
            }

            candidate.AnalyseToken = container.AnalyseToken;
            await _containerWorkChangeGuard.EnsureNoCorrectionAsync(candidate, workChangeParents[candidate.WorkId]);
            await _replacementWriteGuard.EnsureAllowedAsync(
                candidate, workChangeParents[candidate.WorkId], requested.OverrideBlock, storedChange, cancellationToken);
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
