// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Splits a container into two containers in one transaction: the container ends at the split time, a second container
/// starts there for the target person and takes over the children after the split (same ids, nothing is deleted).
/// Nothing is expanded from the container template and no default expenses are added, so the second part carries
/// exactly the moved children and the bases of the container. All checks run before anything is staged: the caller
/// holds the container lock; the container, its children's present and future owners, their replacements and the target
/// are visible; the container is a top-level container that is not sealed (a sealed container is not split, each part
/// is sealed on its own) and that the caller may write, with writable children; the split time is in whole minutes and
/// lies between two children with a task on each side; the days of both persons are open; a service counts with its
/// travel, briefing and debriefing times; the target exists, is an employee, is plannable on the day of the second part
/// and has no hard-blocking conflict. A failing recalculation of the parts rolls the whole split back. After the
/// transaction each part is completed on its own (IScheduleCompletionService.CompleteWorkWriteAsync: the first part against
/// the container before the split, the second part as new), so the overtime successors and the period hours of each part's
/// client are handled in that part's period and a night split into a new period updates both periods; a failure there is
/// logged and repaired by a queued recalculation instead of failing the committed split.
/// </summary>
/// <param name="splitWriter">Stages the shortened container, the second part and the moved children</param>
/// <param name="childrenManager">Recalculates both parts after the structural save</param>

using Klacks.Api.Application.Commands.Works;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Helpers;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Services.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Works;

public class SplitContainerWorkCommandHandler : BaseHandler, IRequestHandler<SplitContainerWorkCommand, SplitContainerWorkResponse>
{
    private readonly IWorkRepository _workRepository;
    private readonly IShiftRepository _shiftRepository;
    private readonly IScheduleRowAccessGuard _rowAccessGuard;
    private readonly IContainerLockRepository _lockRepository;
    private readonly IUserService _userService;
    private readonly IContainerWorkCascadeService _cascadeService;
    private readonly IContainerWorkChildrenReadRepository _childrenReadRepository;
    private readonly IParentWorkLockGuard _workLockGuard;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IDayLockService _dayLockService;
    private readonly IClientPlanningPeriodGuard _planningPeriodGuard;
    private readonly IWorkWriteGuard _workWriteGuard;
    private readonly IContainerSplitWriter _splitWriter;
    private readonly IContainerWorkChildrenManager _childrenManager;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPeriodHoursService _periodHoursService;
    private readonly IScheduleCompletionService _completionService;
    private readonly IWorkNotificationFacade _notificationFacade;
    private readonly ScheduleMapper _scheduleMapper;

    public SplitContainerWorkCommandHandler(
        IWorkRepository workRepository,
        IShiftRepository shiftRepository,
        IScheduleRowAccessGuard rowAccessGuard,
        IContainerLockRepository lockRepository,
        IUserService userService,
        IContainerWorkCascadeService cascadeService,
        IContainerWorkChildrenReadRepository childrenReadRepository,
        IParentWorkLockGuard workLockGuard,
        IHttpContextAccessor httpContextAccessor,
        IDayLockService dayLockService,
        IClientPlanningPeriodGuard planningPeriodGuard,
        IWorkWriteGuard workWriteGuard,
        IContainerSplitWriter splitWriter,
        IContainerWorkChildrenManager childrenManager,
        IUnitOfWork unitOfWork,
        IPeriodHoursService periodHoursService,
        IScheduleCompletionService completionService,
        IWorkNotificationFacade notificationFacade,
        ScheduleMapper scheduleMapper,
        ILogger<SplitContainerWorkCommandHandler> logger)
        : base(logger)
    {
        _workRepository = workRepository;
        _shiftRepository = shiftRepository;
        _rowAccessGuard = rowAccessGuard;
        _lockRepository = lockRepository;
        _userService = userService;
        _cascadeService = cascadeService;
        _childrenReadRepository = childrenReadRepository;
        _workLockGuard = workLockGuard;
        _httpContextAccessor = httpContextAccessor;
        _dayLockService = dayLockService;
        _planningPeriodGuard = planningPeriodGuard;
        _workWriteGuard = workWriteGuard;
        _splitWriter = splitWriter;
        _childrenManager = childrenManager;
        _unitOfWork = unitOfWork;
        _periodHoursService = periodHoursService;
        _completionService = completionService;
        _notificationFacade = notificationFacade;
        _scheduleMapper = scheduleMapper;
    }

    public async Task<SplitContainerWorkResponse> Handle(SplitContainerWorkCommand request, CancellationToken cancellationToken)
    {
        await VerifyLockAsync(request.WorkId, cancellationToken);

        return await ExecuteAsync(async () =>
        {
            var container = await _workRepository.GetNoTracking(request.WorkId);
            if (container == null || container.ParentWorkId != null)
            {
                throw new KeyNotFoundException(string.Format(ContainerLockMessages.WorkNotFound, request.WorkId));
            }

            if (request.SplitTime != new TimeOnly(request.SplitTime.Hour, request.SplitTime.Minute))
            {
                throw new InvalidRequestException(ContainerSplitMessages.SplitTimeInWholeMinutes);
            }

            var children = await _cascadeService.GetLiveChildrenAsync([container.Id], cancellationToken);
            var changes = await _childrenReadRepository.GetWorkChangesForWorks(children.Works.Select(w => w.Id).ToList(), cancellationToken);
            var childShiftClients = await ChildShiftClientsAsync(children, cancellationToken);
            if (!await _rowAccessGuard.AreAllVisibleAsync(
                    TouchedClients(container, children, changes, childShiftClients, request.TargetClientId), container.AnalyseToken, cancellationToken))
            {
                throw new KeyNotFoundException(string.Format(ContainerLockMessages.WorkNotFound, request.WorkId));
            }

            var shift = await _shiftRepository.Get(container.ShiftId);
            if (shift == null || shift.ShiftType != ShiftType.IsContainer)
            {
                throw new InvalidRequestException(ContainerSplitMessages.NotAContainer);
            }

            if (container.LockLevel != WorkLockLevel.None)
            {
                throw new ContainerSplitSealedRefusedException();
            }

            _workLockGuard.EnsureWorkWritableForCaller(container, _httpContextAccessor);
            _workLockGuard.EnsureChildrenWritableForCaller(children, _httpContextAccessor);

            var items = SplitItems(container, children, changes);
            var movedIds = ContainerSplitPartition.ItemsAfter(container.StartTime, container.EndTime, request.SplitTime, items);
            var secondPartDate = ContainerSplitPartition.SecondPartDate(container.CurrentDate, container.StartTime, request.SplitTime);

            await EnsureDaysOpenAsync(container, children, movedIds, secondPartDate, request.TargetClientId, childShiftClients, cancellationToken);
            await _planningPeriodGuard.EnsurePlannablePersonsAsync([request.TargetClientId], cancellationToken);
            await _planningPeriodGuard.EnsureWithinPeriodsAsync([(request.TargetClientId, secondPartDate)], cancellationToken);
            await _workWriteGuard.EnsureNoHardBlockingConflictAsync(
                new Work
                {
                    ClientId = request.TargetClientId,
                    ShiftId = container.ShiftId,
                    CurrentDate = secondPartDate,
                    StartTime = request.SplitTime,
                    EndTime = container.EndTime,
                    AnalyseToken = container.AnalyseToken,
                },
                cancellationToken);

            var plan = new ContainerSplitPlan(container.Id, request.SplitTime, secondPartDate, request.TargetClientId, movedIds);
            var (firstPart, secondPart) = await _unitOfWork.ExecuteInTransactionAsync(() => WriteAsync(plan, cancellationToken));

            return await CompleteAsync(container, firstPart, secondPart, cancellationToken);
        },
        "splitting a container",
        new { request.WorkId, request.SplitTime, request.TargetClientId });
    }

    private async Task<(Work FirstPart, Work SecondPart)> WriteAsync(ContainerSplitPlan plan, CancellationToken cancellationToken)
    {
        _unitOfWork.DiscardTrackedChanges();
        var secondPart = await _splitWriter.StageAsync(plan, cancellationToken);
        await _unitOfWork.CompleteAsync();

        var firstPart = await _workRepository.Get(plan.ContainerId)
            ?? throw new KeyNotFoundException(string.Format(ContainerLockMessages.WorkNotFound, plan.ContainerId));
        var calculated = await _childrenManager.RecalculateForNewOwnerAsync(firstPart, cancellationToken)
            & await _childrenManager.RecalculateForNewOwnerAsync(secondPart, cancellationToken);
        if (!calculated)
        {
            throw new InvalidRequestException(ContainerSplitMessages.RecalculationFailed);
        }

        await _unitOfWork.CompleteAsync();
        return (firstPart, secondPart);
    }

    private async Task<SplitContainerWorkResponse> CompleteAsync(
        Work before, Work firstPart, Work secondPart, CancellationToken cancellationToken)
    {
        var firstPeriod = await _periodHoursService.GetPeriodBoundariesAsync(firstPart.CurrentDate);
        var secondPeriod = secondPart.CurrentDate == firstPart.CurrentDate
            ? firstPeriod
            : await _periodHoursService.GetPeriodBoundariesAsync(secondPart.CurrentDate);
        await _completionService.CompleteWorkWriteAsync(firstPart, before, firstPeriod.StartDate, firstPeriod.EndDate);
        await _completionService.CompleteWorkWriteAsync(secondPart, null, secondPeriod.StartDate, secondPeriod.EndDate);

        var connectionId = _notificationFacade.GetConnectionId();
        await _notificationFacade.NotifyWorkUpdatedAsync(firstPart, connectionId, firstPeriod.StartDate, firstPeriod.EndDate);
        await _notificationFacade.NotifyWorkCreatedAsync(secondPart, connectionId, secondPeriod.StartDate, secondPeriod.EndDate);

        var affectedShifts = new HashSet<(Guid ShiftId, DateOnly Date)>
        {
            (firstPart.ShiftId, firstPart.CurrentDate),
            (secondPart.ShiftId, secondPart.CurrentDate),
        };
        await _notificationFacade.NotifyShiftStatsAsync(affectedShifts, connectionId, firstPart.AnalyseToken, cancellationToken);

        return new SplitContainerWorkResponse
        {
            FirstPart = _scheduleMapper.ToWorkResource(firstPart),
            SecondPart = _scheduleMapper.ToWorkResource(secondPart),
        };
    }

    /// <summary>A service occupies its own span widened by its travel, briefing and debriefing times, so a split never cuts those off.</summary>
    private static List<ContainerSplitItem> SplitItems(Work container, ContainerChildren children, IEnumerable<WorkChange> workChanges)
    {
        var changes = workChanges.ToLookup(change => change.WorkId);
        return children.Works
            .Select(w =>
            {
                var (start, end) = ContainerSplitPartition.Occupied(
                    container.StartTime, w.StartTime, w.EndTime, changes[w.Id].Select(c => (c.StartTime, c.EndTime)));
                return new ContainerSplitItem(w.Id, start, end, true);
            })
            .Concat(children.Breaks.Select(b => new ContainerSplitItem(b.Id, b.StartTime, b.EndTime, false)))
            .ToList();
    }

    /// <summary>The customer of each child's shift, which owns the child after the split (ContainerChildOwner).</summary>
    private async Task<Dictionary<Guid, Guid?>> ChildShiftClientsAsync(ContainerChildren children, CancellationToken cancellationToken)
    {
        var shiftIds = children.Works.Select(w => w.ShiftId).Distinct().ToList();
        var shifts = await _shiftRepository.GetWithClientByIdsAsync(shiftIds, cancellationToken);
        return shifts.ToDictionary(s => s.Id, s => s.ClientId);
    }

    /// <summary>Every client whose rows the split rewrites: the container, the children with their replacements and future owners, the target.</summary>
    private static List<Guid> TouchedClients(
        Work container,
        ContainerChildren children,
        IEnumerable<WorkChange> changes,
        IReadOnlyDictionary<Guid, Guid?> childShiftClients,
        Guid targetClientId)
        => children.Works.Select(w => w.ClientId)
            .Concat(children.Works.Select(w => ContainerChildOwner.Of(childShiftClients.GetValueOrDefault(w.ShiftId), targetClientId)))
            .Concat(children.Breaks.Select(b => b.ClientId))
            .Concat(changes.Where(c => c.ReplaceClientId.HasValue).Select(c => c.ReplaceClientId!.Value))
            .Append(container.ClientId)
            .Append(targetClientId)
            .Distinct()
            .ToList();

    private async Task VerifyLockAsync(Guid workId, CancellationToken cancellationToken)
    {
        var userId = _userService.GetId() ?? Guid.Empty;
        var instanceId = _userService.GetInstanceId() ?? string.Empty;
        if (!await _lockRepository.IsHeldBy(ContainerLockResourceTypes.ContainerWork, workId, userId, instanceId, cancellationToken))
        {
            throw new ContainerLockedException(ContainerSplitMessages.NotLockedBySession);
        }
    }

    private async Task EnsureDaysOpenAsync(
        Work container,
        ContainerChildren children,
        IReadOnlySet<Guid> movedIds,
        DateOnly secondPartDate,
        Guid targetClientId,
        IReadOnlyDictionary<Guid, Guid?> childShiftClients,
        CancellationToken cancellationToken)
    {
        var moved = children.Works.Where(w => movedIds.Contains(w.Id)).ToList();
        var days = new HashSet<(DateOnly Date, Guid ClientId, Guid? AnalyseToken)>
        {
            (container.CurrentDate, container.ClientId, container.AnalyseToken),
            (secondPartDate, targetClientId, container.AnalyseToken),
        };
        days.UnionWith(moved.Select(w => (w.CurrentDate, w.ClientId, w.AnalyseToken)));
        days.UnionWith(moved.Select(w => (secondPartDate, ContainerChildOwner.Of(childShiftClients.GetValueOrDefault(w.ShiftId), targetClientId), w.AnalyseToken)));
        days.UnionWith(children.Breaks.Where(b => movedIds.Contains(b.Id)).Select(b => (b.CurrentDate, b.ClientId, b.AnalyseToken)));
        await _dayLockService.EnsureNoneLockedAsync(days.ToList(), cancellationToken);
    }
}
