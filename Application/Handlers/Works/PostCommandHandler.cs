// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Creates a Work: day-lock and write guards first, then the Work with its container expansion and
/// default expenses, commit, completion (overtime successors and period hours, IScheduleCompletionService.CompleteWorkWriteAsync;
/// a failure there is logged and repaired by a queued recalculation instead of failing the committed create), notifications and the three-day WorkResource the client repaints from. A Work for a client outside the caller's group visibility is
/// refused exactly like a Work for a client that does not exist; nothing is written. Like the grid's locked cells, a
/// Work on a day outside the client's membership or group period is refused; container children follow their parent's
/// cell and are not checked on their own. A Work on a shift outside the caller's group visibility is refused exactly like a
/// Work on a shift that does not exist (404, same text), before any other check reveals anything about the shift, and so
/// is a Work on a shift of another world (IWorkWriteGuard, a key shift or a clone of another scenario).
/// </summary>
/// <param name="request">Carries the WorkResource to persist</param>
/// <param name="clientVisibilityGuard">Decides whether the calling user may write for the client</param>
/// <param name="defaultExpensesApplier">Stages the shift's default expenses on the new Work (same scope, same save)</param>
/// <param name="planningPeriodGuard">Refuses a Work whose client is not plannable on its day (membership or group period), and a top-level Work for a missing client or a customer</param>
/// <param name="shiftVisibilityGuard">Decides whether the calling user may book the Work's shift</param>

using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Application.Services.Clients;
using Klacks.Api.Application.Services.Shifts;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.Handlers.Works;

public class PostCommandHandler : BaseHandler, IRequestHandler<PostCommand<WorkResource>, WorkResource?>
{
    private readonly IWorkRepository _workRepository;
    private readonly IClientVisibilityGuard _clientVisibilityGuard;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IPeriodHoursService _periodHoursService;
    private readonly IScheduleEntriesService _scheduleEntriesService;
    private readonly IScheduleCompletionService _completionService;
    private readonly IWorkNotificationFacade _notificationFacade;
    private readonly IShiftDefaultExpensesApplier _defaultExpensesApplier;
    private readonly IContainerWorkExpansionService _expansionService;
    private readonly ISelectedGroupContextResolver _groupContextResolver;
    private readonly IDayLockService _dayLockService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWorkWriteGuard _writeGuard;
    private readonly IClientPlanningPeriodGuard _planningPeriodGuard;
    private readonly IShiftVisibilityGuard _shiftVisibilityGuard;

    public PostCommandHandler(
        IWorkRepository workRepository,
        IClientVisibilityGuard clientVisibilityGuard,
        ScheduleMapper scheduleMapper,
        IPeriodHoursService periodHoursService,
        IScheduleEntriesService scheduleEntriesService,
        IScheduleCompletionService completionService,
        IWorkNotificationFacade notificationFacade,
        IShiftDefaultExpensesApplier defaultExpensesApplier,
        IContainerWorkExpansionService expansionService,
        ISelectedGroupContextResolver groupContextResolver,
        IDayLockService dayLockService,
        IUnitOfWork unitOfWork,
        IWorkWriteGuard writeGuard,
        IClientPlanningPeriodGuard planningPeriodGuard,
        IShiftVisibilityGuard shiftVisibilityGuard,
        ILogger<PostCommandHandler> logger)
        : base(logger)
    {
        _workRepository = workRepository;
        _clientVisibilityGuard = clientVisibilityGuard;
        _scheduleMapper = scheduleMapper;
        _periodHoursService = periodHoursService;
        _scheduleEntriesService = scheduleEntriesService;
        _completionService = completionService;
        _notificationFacade = notificationFacade;
        _defaultExpensesApplier = defaultExpensesApplier;
        _expansionService = expansionService;
        _groupContextResolver = groupContextResolver;
        _dayLockService = dayLockService;
        _unitOfWork = unitOfWork;
        _writeGuard = writeGuard;
        _planningPeriodGuard = planningPeriodGuard;
        _shiftVisibilityGuard = shiftVisibilityGuard;
    }

    public async Task<WorkResource?> Handle(PostCommand<WorkResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var work = _scheduleMapper.ToWorkEntity(request.Resource);

            if (!await _clientVisibilityGuard.IsVisibleAsync(work.ClientId, cancellationToken))
            {
                throw new KeyNotFoundException(string.Format(ClientVisibilityMessages.ClientNotFound, work.ClientId));
            }

            if (!await _shiftVisibilityGuard.IsVisibleAsync(work.ShiftId, cancellationToken))
            {
                throw new KeyNotFoundException(string.Format(ShiftVisibilityMessages.ShiftNotFound, work.ShiftId));
            }

            await _writeGuard.EnsureShiftsInWorkWorldAsync([(work.ShiftId, work.AnalyseToken)], cancellationToken);

            await _dayLockService.EnsureNotLockedAsync(
                work.CurrentDate,
                work.ClientId,
                work.AnalyseToken,
                cancellationToken);

            if (work.ParentWorkId == null)
            {
                await _planningPeriodGuard.EnsurePlannablePersonsAsync([work.ClientId], cancellationToken);
            }

            await _planningPeriodGuard.EnsureWithinPeriodsAsync([(work.ClientId, work.CurrentDate)], cancellationToken);

            await _writeGuard.EnsureNoSporadicConflictAsync(work, cancellationToken);

            await _writeGuard.EnsureNoHardBlockingConflictAsync(work, cancellationToken);

            var (periodStart, periodEnd) = await _periodHoursService.GetPeriodBoundariesAsync(work.CurrentDate);

            await _workRepository.Add(work);
            await _expansionService.ExpandAsync(work, work.CurrentDate);

            await _defaultExpensesApplier.ApplyAsync([work], cancellationToken);

            await _unitOfWork.CompleteAsync();
            var completion = await _completionService.CompleteWorkWriteAsync(work, null, periodStart, periodEnd);

            var connectionId = _notificationFacade.GetConnectionId();
            await _notificationFacade.NotifyWorkCreatedAsync(work, connectionId, periodStart, periodEnd);
            await _notificationFacade.NotifyShiftStatsAsync(work.ShiftId, work.CurrentDate, connectionId, work.AnalyseToken, cancellationToken);

            var visibleGroupIds = await _groupContextResolver.ResolveVisibleGroupIdsAsync();
            var scheduleEntries = await _scheduleEntriesService.GetScheduleEntriesAsync(
                ScheduleEntriesCriteria.RefreshWindowAround(
                    work.CurrentDate, [work.ClientId], visibleGroupIds, work.AnalyseToken),
                cancellationToken);

            var workResource = _scheduleMapper.ToWorkResource(work);
            workResource.PeriodHours = completion.PeriodHours;
            workResource.ScheduleEntries = scheduleEntries.Select(_scheduleMapper.ToWorkScheduleResource).ToList();

            return workResource;
        }, "CreateWork", new { request.Resource.ClientId });
    }
}
