// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Creates a Work: day-lock and write guards first, then the Work with its container expansion and
/// default expenses, commit, overtime successors, period hours, notifications and the three-day
/// WorkResource the client repaints from. A Work for a client outside the caller's group visibility is
/// refused exactly like a Work for a client that does not exist; nothing is written.
/// </summary>
/// <param name="request">Carries the WorkResource to persist</param>
/// <param name="clientVisibilityGuard">Decides whether the calling user may write for the client</param>

using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.EntityFrameworkCore;

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
    private readonly IShiftExpensesRepository _shiftExpensesRepository;
    private readonly IExpensesRepository _expensesRepository;
    private readonly IContainerWorkExpansionService _expansionService;
    private readonly ISelectedGroupContextResolver _groupContextResolver;
    private readonly IDayLockService _dayLockService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOvertimeCascadeService _overtimeCascadeService;
    private readonly IWorkWriteGuard _writeGuard;

    public PostCommandHandler(
        IWorkRepository workRepository,
        IClientVisibilityGuard clientVisibilityGuard,
        ScheduleMapper scheduleMapper,
        IPeriodHoursService periodHoursService,
        IScheduleEntriesService scheduleEntriesService,
        IScheduleCompletionService completionService,
        IWorkNotificationFacade notificationFacade,
        IShiftExpensesRepository shiftExpensesRepository,
        IExpensesRepository expensesRepository,
        IContainerWorkExpansionService expansionService,
        ISelectedGroupContextResolver groupContextResolver,
        IDayLockService dayLockService,
        IUnitOfWork unitOfWork,
        IOvertimeCascadeService overtimeCascadeService,
        IWorkWriteGuard writeGuard,
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
        _shiftExpensesRepository = shiftExpensesRepository;
        _expensesRepository = expensesRepository;
        _expansionService = expansionService;
        _groupContextResolver = groupContextResolver;
        _dayLockService = dayLockService;
        _unitOfWork = unitOfWork;
        _overtimeCascadeService = overtimeCascadeService;
        _writeGuard = writeGuard;
    }

    public async Task<WorkResource?> Handle(PostCommand<WorkResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var work = _scheduleMapper.ToWorkEntity(request.Resource);

            if (!await _clientVisibilityGuard.IsVisibleAsync(work.ClientId, cancellationToken))
            {
                throw new KeyNotFoundException($"Client with ID {work.ClientId} not found");
            }

            await _dayLockService.EnsureNotLockedAsync(
                work.CurrentDate,
                work.ClientId,
                work.AnalyseToken,
                cancellationToken);

            await _writeGuard.EnsureNoSporadicConflictAsync(work, cancellationToken);

            await _writeGuard.EnsureNoHardBlockingConflictAsync(work, cancellationToken);

            var (periodStart, periodEnd) = await _periodHoursService.GetPeriodBoundariesAsync(work.CurrentDate);

            await _workRepository.Add(work);
            await _expansionService.ExpandAsync(work, work.CurrentDate);

            var defaultExpenses = await _shiftExpensesRepository.GetByShiftId(work.ShiftId);
            foreach (var defaultExpense in defaultExpenses)
            {
                var expense = new Domain.Models.Schedules.Expenses
                {
                    WorkId = work.Id,
                    Amount = defaultExpense.Amount,
                    Description = defaultExpense.Description,
                    Taxable = defaultExpense.Taxable
                };
                await _expensesRepository.Add(expense);
            }

            // K3/K4 cascade: commit the new Work first, then reprocess the successor Works in its
            // overtime basis period (their prior-hours sums read committed database state, never the
            // change tracker) BEFORE the completion service recalculates period hours, so the stored
            // ClientPeriodHours already include the successors' adjusted surcharges.
            await _unitOfWork.CompleteAsync();
            await _overtimeCascadeService.ReprocessSuccessorsAsync(work);

            var periodHours = await _completionService.SaveAndTrackAsync(
                work.ClientId, work.CurrentDate, periodStart, periodEnd, work.AnalyseToken);

            var connectionId = _notificationFacade.GetConnectionId();
            await _notificationFacade.NotifyWorkCreatedAsync(work, connectionId, periodStart, periodEnd);
            await _notificationFacade.NotifyPeriodHoursUpdatedAsync(work.ClientId, periodStart, periodEnd, periodHours, connectionId, work.AnalyseToken);
            await _notificationFacade.NotifyShiftStatsAsync(work.ShiftId, work.CurrentDate, connectionId, work.AnalyseToken, cancellationToken);

            var currentDate = work.CurrentDate;
            var threeDayStart = currentDate.AddDays(-1);
            var threeDayEnd = currentDate.AddDays(1);

            var visibleGroupIds = await _groupContextResolver.ResolveVisibleGroupIdsAsync();
            var scheduleEntries = await _scheduleEntriesService
                .GetScheduleEntriesQuery(threeDayStart, threeDayEnd, visibleGroupIds, work.AnalyseToken)
                .Where(e => e.ClientId == work.ClientId)
                .ToListAsync(cancellationToken);

            var workResource = _scheduleMapper.ToWorkResource(work);
            workResource.PeriodHours = periodHours;
            workResource.ScheduleEntries = scheduleEntries.Select(_scheduleMapper.ToWorkScheduleResource).ToList();

            return workResource;
        }, "CreateWork", new { request.Resource.ClientId });
    }
}
