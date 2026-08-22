// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Exceptions;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Services.Schedules;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Application.Handlers.Works;

public class PutCommandHandler : BaseHandler, IRequestHandler<PutCommand<WorkResource>, WorkResource?>
{
    private readonly IWorkRepository _workRepository;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IPeriodHoursService _periodHoursService;
    private readonly IScheduleEntriesService _scheduleEntriesService;
    private readonly IScheduleCompletionService _completionService;
    private readonly IWorkNotificationFacade _notificationFacade;
    private readonly IContainerWorkCascadeService _cascadeService;
    private readonly ISelectedGroupContextResolver _groupContextResolver;
    private readonly IDayLockService _dayLockService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOvertimeCascadeService _overtimeCascadeService;
    private readonly IPreCommitConflictChecker _conflictChecker;

    public PutCommandHandler(
        IWorkRepository workRepository,
        ScheduleMapper scheduleMapper,
        IPeriodHoursService periodHoursService,
        IScheduleEntriesService scheduleEntriesService,
        IScheduleCompletionService completionService,
        IWorkNotificationFacade notificationFacade,
        IContainerWorkCascadeService cascadeService,
        ISelectedGroupContextResolver groupContextResolver,
        IDayLockService dayLockService,
        IUnitOfWork unitOfWork,
        IOvertimeCascadeService overtimeCascadeService,
        IPreCommitConflictChecker conflictChecker,
        ILogger<PutCommandHandler> logger)
        : base(logger)
    {
        _workRepository = workRepository;
        _scheduleMapper = scheduleMapper;
        _periodHoursService = periodHoursService;
        _scheduleEntriesService = scheduleEntriesService;
        _completionService = completionService;
        _notificationFacade = notificationFacade;
        _cascadeService = cascadeService;
        _groupContextResolver = groupContextResolver;
        _dayLockService = dayLockService;
        _unitOfWork = unitOfWork;
        _overtimeCascadeService = overtimeCascadeService;
        _conflictChecker = conflictChecker;
    }

    public async Task<WorkResource?> Handle(PutCommand<WorkResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var existingWork = await _workRepository.GetNoTracking(request.Resource.Id);
            var oldShiftId = existingWork?.ShiftId;
            var oldDate = existingWork?.CurrentDate;

            var work = _scheduleMapper.ToWorkEntity(request.Resource);
            ScheduleEntrySealState.CarryOver(work, existingWork);

            if (oldDate.HasValue)
            {
                await _dayLockService.EnsureNotLockedAsync(
                    oldDate.Value,
                    existingWork!.ClientId,
                    work.AnalyseToken,
                    cancellationToken);
            }

            await _dayLockService.EnsureNotLockedAsync(
                work.CurrentDate,
                work.ClientId,
                work.AnalyseToken,
                cancellationToken);

            await EnsureNoHardBlockingConflictAsync(work, existingWork, cancellationToken);

            var (periodStart, periodEnd) = await _periodHoursService.GetPeriodBoundariesAsync(work.CurrentDate);

            var updatedWork = await _workRepository.Put(work);
            if (updatedWork == null) return null;

            var dateChanged = oldDate.HasValue && oldDate.Value != updatedWork.CurrentDate;
            var clientChanged = existingWork != null && existingWork.ClientId != updatedWork.ClientId;
            if (dateChanged || clientChanged)
            {
                await _cascadeService.MoveChildrenAsync(updatedWork.Id, updatedWork.CurrentDate, updatedWork.ClientId);
            }

            // No lock-level cascade here any more: the seal state is server-owned and carried over
            // unchanged above, so an edit can never change it. Seal changes cascade from the handlers that
            // enforce CanSeal (ConfirmWorkCommandHandler) or write the whole range in bulk.

            // K3/K4 cascade: commit the edit first, then reprocess the successor Works of both the new
            // position and (when client/date/start time/scenario changed) the old position — their
            // prior-hours sums read committed database state — BEFORE period hours are recalculated.
            await _unitOfWork.CompleteAsync();
            await _overtimeCascadeService.ReprocessSuccessorsAsync(updatedWork, existingWork);

            var periodHours = await _completionService.SaveAndTrackMoveAsync(
                updatedWork.ClientId, updatedWork.CurrentDate, periodStart, periodEnd,
                existingWork?.ClientId, existingWork?.CurrentDate, updatedWork.AnalyseToken);

            var connectionId = _notificationFacade.GetConnectionId();
            await _notificationFacade.NotifyWorkUpdatedAsync(updatedWork, connectionId, periodStart, periodEnd);
            await _notificationFacade.NotifyPeriodHoursUpdatedAsync(updatedWork.ClientId, periodStart, periodEnd, periodHours, connectionId, updatedWork.AnalyseToken);

            var affectedShifts = new HashSet<(Guid ShiftId, DateOnly Date)>
            {
                (updatedWork.ShiftId, updatedWork.CurrentDate)
            };

            if (oldShiftId.HasValue && oldDate.HasValue &&
                (oldShiftId.Value != updatedWork.ShiftId || oldDate.Value != updatedWork.CurrentDate))
            {
                affectedShifts.Add((oldShiftId.Value, oldDate.Value));
            }

            await _notificationFacade.NotifyShiftStatsAsync(affectedShifts, connectionId, updatedWork.AnalyseToken, cancellationToken);

            var currentDate = updatedWork.CurrentDate;
            var threeDayStart = currentDate.AddDays(-1);
            var threeDayEnd = currentDate.AddDays(1);

            var visibleGroupIds = await _groupContextResolver.ResolveVisibleGroupIdsAsync();
            var scheduleEntries = await _scheduleEntriesService
                .GetScheduleEntriesQuery(threeDayStart, threeDayEnd, visibleGroupIds, updatedWork.AnalyseToken)
                .Where(e => e.ClientId == updatedWork.ClientId)
                .ToListAsync(cancellationToken);

            var workResource = _scheduleMapper.ToWorkResource(updatedWork);
            workResource.PeriodHours = periodHours;
            workResource.ScheduleEntries = scheduleEntries.Select(_scheduleMapper.ToWorkScheduleResource).ToList();

            return workResource;
        }, "UpdateWork", new { request.Resource.Id });
    }

    /// <summary>
    /// Refuses an edit only for the one Error class that can never be reported after the fact instead:
    /// a missing mandatory qualification. Same severity rule as the create path otherwise: a rule
    /// configured as Block still reports into the error list instead of stopping the write, and so does
    /// a schedule collision (owner decision 2026-08-22) - it is persisted and the async post-commit
    /// check surfaces it into the error list rather than refusing the edit.
    /// The PROJECTED row is checked and the pre-write row is handed over as a removal - without it every
    /// retiming would collide with its own still-persisted predecessor.
    /// Scenario writes are skipped, mirroring the day-lock contract, and so are container children: the
    /// checker's world contains parent works only, so a child would be judged against a baseline that
    /// excludes it and would collide with its own container.
    /// </summary>
    /// <param name="work">The projected row (new client, date and times)</param>
    /// <param name="existingWork">The row as persisted before the edit; null when there is nothing to update</param>
    private async Task EnsureNoHardBlockingConflictAsync(
        Domain.Models.Schedules.Work work,
        Domain.Models.Schedules.Work? existingWork,
        CancellationToken cancellationToken)
    {
        if (work.AnalyseToken != null || existingWork == null || existingWork.ParentWorkId != null)
        {
            return;
        }

        var plannedRow = new PlannedWorkRow(
            work.ClientId,
            work.CurrentDate,
            work.StartTime,
            work.EndTime,
            work.ShiftId);

        var vacatedRow = new PlannedRemovalRow(
            existingWork.ClientId,
            existingWork.CurrentDate,
            existingWork.StartTime,
            existingWork.EndTime,
            existingWork.Id);

        var conflictCheck = await _conflictChecker.CheckAsync(
            [plannedRow], [vacatedRow], null, cancellationToken);
        if (conflictCheck.HasHardBlocking)
        {
            throw new ConflictException(
                $"Work update blocked: client {work.ClientId} would introduce " +
                $"{conflictCheck.NewConflicts.Count(c => c.Type == ScheduleValidationType.Error)} " +
                $"non-overridable schedule conflict(s) on {work.CurrentDate:yyyy-MM-dd}. Not committed.");
        }
    }
}
