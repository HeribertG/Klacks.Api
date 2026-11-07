// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Hands a Work over to another client: day-lock and hard-blocking guards for both clients, the hand-over including
/// container children and the recalculation for the new owner (IWorkHandOverService), commit, overtime successors,
/// period hours and the schedule entries of source and target. A Work whose current owner or whose target client is
/// outside the caller's group visibility is refused exactly like a Work that does not exist; nothing is written. Like
/// the Work PUT and DELETE, the stored Work must be writable for the caller under IParentWorkLockGuard: a Closed Work
/// is refused for everyone, an Approved one needs Admin or Authorised. Like the grid's drop target, the target client
/// must be plannable on the Work's day: a day outside its membership or group period is refused, and a missing or
/// deleted target or a customer is refused. The container children it moves along must be writable for the caller too;
/// that is checked before the first change is staged, so a refusal leaves nothing behind. A Work on a shift of another
/// world reads like a missing shift (IWorkWriteGuard). A recalculation that fails for the new owner refuses the
/// reassignment; nothing is written. A target equal to the origin of a moved Work takes the move back
/// (IMovedWorkReversal).
/// </summary>
/// <param name="rowAccessGuard">Decides whether the calling user may see the owning client and the stored row's scenario</param>
/// <param name="httpContextAccessor">Source of the calling user's roles for the Work lock rule</param>
/// <param name="workLockGuard">Refuses the reassignment when the Work's lock level forbids it for the caller</param>
/// <param name="planningPeriodGuard">Refuses a target client whose membership or group period does not cover the Work's day</param>
/// <param name="movedWorkReversal">Takes a move back when the target is the origin of the Work</param>

using Klacks.Api.Application.Commands.Works;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Exceptions;
using Klacks.Api.Application.Helpers;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.Handlers.Works;

public class ReassignWorkClientCommandHandler : BaseHandler, IRequestHandler<ReassignWorkClientCommand, ReassignWorkClientResponse?>
{
    private const string AlreadyWithTargetMessage = "This service is already with this person.";
    private const string RecalculationFailedMessage =
        "The work could not be recalculated for the new owner, so it was not reassigned. Check the macro of its shift.";

    private readonly IWorkRepository _workRepository;
    private readonly IScheduleRowAccessGuard _rowAccessGuard;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IPeriodHoursService _periodHoursService;
    private readonly IScheduleEntriesService _scheduleEntriesService;
    private readonly IScheduleCompletionService _completionService;
    private readonly IWorkNotificationFacade _notificationFacade;
    private readonly IContainerWorkCascadeService _cascadeService;
    private readonly IWorkHandOverService _handOverService;
    private readonly ISelectedGroupContextResolver _groupContextResolver;
    private readonly IDayLockService _dayLockService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOvertimeCascadeService _overtimeCascadeService;
    private readonly IWorkWriteGuard _workWriteGuard;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IParentWorkLockGuard _workLockGuard;
    private readonly IClientPlanningPeriodGuard _planningPeriodGuard;
    private readonly IMovedWorkReversal _movedWorkReversal;

    public ReassignWorkClientCommandHandler(
        IWorkRepository workRepository,
        IScheduleRowAccessGuard rowAccessGuard,
        ScheduleMapper scheduleMapper,
        IPeriodHoursService periodHoursService,
        IScheduleEntriesService scheduleEntriesService,
        IScheduleCompletionService completionService,
        IWorkNotificationFacade notificationFacade,
        IContainerWorkCascadeService cascadeService,
        IWorkHandOverService handOverService,
        ISelectedGroupContextResolver groupContextResolver,
        IDayLockService dayLockService,
        IUnitOfWork unitOfWork,
        IOvertimeCascadeService overtimeCascadeService,
        IWorkWriteGuard workWriteGuard,
        IHttpContextAccessor httpContextAccessor,
        IParentWorkLockGuard workLockGuard,
        IClientPlanningPeriodGuard planningPeriodGuard,
        IMovedWorkReversal movedWorkReversal,
        ILogger<ReassignWorkClientCommandHandler> logger)
        : base(logger)
    {
        _workRepository = workRepository;
        _rowAccessGuard = rowAccessGuard;
        _scheduleMapper = scheduleMapper;
        _periodHoursService = periodHoursService;
        _scheduleEntriesService = scheduleEntriesService;
        _completionService = completionService;
        _notificationFacade = notificationFacade;
        _cascadeService = cascadeService;
        _handOverService = handOverService;
        _groupContextResolver = groupContextResolver;
        _dayLockService = dayLockService;
        _unitOfWork = unitOfWork;
        _overtimeCascadeService = overtimeCascadeService;
        _workWriteGuard = workWriteGuard;
        _httpContextAccessor = httpContextAccessor;
        _workLockGuard = workLockGuard;
        _planningPeriodGuard = planningPeriodGuard;
        _movedWorkReversal = movedWorkReversal;
    }

    public async Task<ReassignWorkClientResponse?> Handle(ReassignWorkClientCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var existingWork = await _workRepository.GetNoTracking(request.Id);
            if (existingWork == null
                || !await _rowAccessGuard.AreAllVisibleAsync(
                    new[] { existingWork.ClientId, request.TargetClientId }, existingWork.AnalyseToken, cancellationToken))
            {
                throw new KeyNotFoundException($"Work with ID {request.Id} not found.");
            }

            if (existingWork.ClientId == request.TargetClientId)
            {
                throw new InvalidRequestException(AlreadyWithTargetMessage);
            }

            await _planningPeriodGuard.EnsurePlannablePersonsAsync([request.TargetClientId], cancellationToken);

            await _workWriteGuard.EnsureShiftsInWorkWorldAsync(
                [(existingWork.ShiftId, existingWork.AnalyseToken)], cancellationToken);

            _workLockGuard.EnsureWorkWritableForCaller(existingWork, _httpContextAccessor);

            await _dayLockService.EnsureNotLockedAsync(
                existingWork.CurrentDate, existingWork.ClientId, existingWork.AnalyseToken, cancellationToken);
            await _dayLockService.EnsureNotLockedAsync(
                existingWork.CurrentDate, request.TargetClientId, existingWork.AnalyseToken, cancellationToken);

            await _planningPeriodGuard.EnsureWithinPeriodsAsync(
                [(request.TargetClientId, existingWork.CurrentDate)], cancellationToken);

            await EnsureNoHardBlockingConflictAsync(existingWork, request.TargetClientId, cancellationToken);

            var children = await _cascadeService.GetLiveChildrenAsync([existingWork.Id], cancellationToken);
            _workLockGuard.EnsureChildrenWritableForCaller(children, _httpContextAccessor);

            var work = await _workRepository.Get(request.Id);
            if (work == null)
            {
                throw new KeyNotFoundException($"Work with ID {request.Id} not found.");
            }

            await _movedWorkReversal.StageAsync(work, request.TargetClientId, cancellationToken);
            if (!await _handOverService.TransferAsync(work, request.TargetClientId, null, cancellationToken))
            {
                _unitOfWork.DiscardTrackedChanges();
                throw new InvalidRequestException(RecalculationFailedMessage);
            }

            var (periodStart, periodEnd) = await _periodHoursService.GetPeriodBoundariesAsync(work.CurrentDate);

            // K3/K4 cascade: commit the reassignment first, then reprocess successors of both the old and
            // new client position (their prior-hours sums read committed database state) BEFORE period
            // hours are recalculated — same ordering contract as PutCommandHandler.
            await _unitOfWork.CompleteAsync();
            await _overtimeCascadeService.ReprocessSuccessorsAsync(work, existingWork);

            var (periodHours, previousPeriodHours) = await _completionService.SaveAndTrackMoveAsync(
                work.ClientId, work.CurrentDate, periodStart, periodEnd,
                existingWork.ClientId, existingWork.CurrentDate, work.AnalyseToken);

            var connectionId = _notificationFacade.GetConnectionId();
            await _notificationFacade.NotifyWorkUpdatedAsync(work, connectionId, periodStart, periodEnd);

            var affectedShifts = new HashSet<(Guid ShiftId, DateOnly Date)> { (work.ShiftId, work.CurrentDate) };
            await _notificationFacade.NotifyShiftStatsAsync(affectedShifts, connectionId, work.AnalyseToken, cancellationToken);

            var visibleGroupIds = await _groupContextResolver.ResolveVisibleGroupIdsAsync();

            var allEntries = await _scheduleEntriesService.GetScheduleEntriesAsync(
                ScheduleEntriesCriteria.RefreshWindowAround(
                    work.CurrentDate, [work.ClientId, existingWork.ClientId], visibleGroupIds, work.AnalyseToken),
                cancellationToken);

            var workResource = _scheduleMapper.ToWorkResource(work);
            workResource.PeriodHours = periodHours;
            workResource.ScheduleEntries = allEntries
                .Where(e => e.ClientId == work.ClientId)
                .Select(_scheduleMapper.ToWorkScheduleResource)
                .ToList();

            return new ReassignWorkClientResponse
            {
                Work = workResource,
                SourceScheduleEntries = allEntries
                    .Where(e => e.ClientId == existingWork.ClientId)
                    .Select(_scheduleMapper.ToWorkScheduleResource)
                    .ToList(),
                SourcePeriodHours = previousPeriodHours,
            };
        },
        "reassigning work to another client",
        new { WorkId = request.Id, request.TargetClientId });
    }

    /// <summary>
    /// Refuses the reassignment only for the one Error class that can never be reported after the fact
    /// instead: a missing mandatory qualification for the target client. Same severity rule as the
    /// create path otherwise: a Block-mode compliance rule still reports into the error list rather than
    /// stopping the write, and so does a schedule collision with the target client's existing schedule
    /// (owner decision 2026-08-22) - the reassignment persists and the async post-commit check surfaces
    /// it into the error list.
    /// The target client is checked with the shift's existing times, and the source client's row is
    /// handed over as a removal so the source is judged on the state it is left in rather than on a
    /// double-booked intermediate.
    /// Scenario writes are skipped, mirroring the day-lock contract, and so are container children: the
    /// checker's world contains parent works only, so a child would collide with its own container.
    /// </summary>
    /// <param name="existingWork">The work as persisted, supplying date, times, shift and source client</param>
    /// <param name="targetClientId">Client the work would be handed to</param>
    private async Task EnsureNoHardBlockingConflictAsync(
        Domain.Models.Schedules.Work existingWork,
        Guid targetClientId,
        CancellationToken cancellationToken)
    {
        if (existingWork.AnalyseToken != null || existingWork.ParentWorkId != null)
        {
            return;
        }

        var plannedRow = new PlannedWorkRow(
            targetClientId,
            existingWork.CurrentDate,
            existingWork.StartTime,
            existingWork.EndTime,
            existingWork.ShiftId);

        var vacatedRow = new PlannedRemovalRow(
            existingWork.ClientId,
            existingWork.CurrentDate,
            existingWork.StartTime,
            existingWork.EndTime,
            existingWork.Id);

        var refusal = await _workWriteGuard.FindHardBlockingRefusalAsync(
            [plannedRow], [vacatedRow], WorkWriteOperation.Reassign, cancellationToken);
        if (refusal != null)
        {
            throw WorkWriteConflictException.From(refusal);
        }
    }
}
