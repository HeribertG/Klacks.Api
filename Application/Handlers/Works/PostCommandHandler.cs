// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Exceptions;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Services.Shifts;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Application.Handlers.Works;

public class PostCommandHandler : BaseHandler, IRequestHandler<PostCommand<WorkResource>, WorkResource?>
{
    private readonly IWorkRepository _workRepository;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IPeriodHoursService _periodHoursService;
    private readonly IScheduleEntriesService _scheduleEntriesService;
    private readonly IScheduleCompletionService _completionService;
    private readonly IWorkNotificationFacade _notificationFacade;
    private readonly IShiftExpensesRepository _shiftExpensesRepository;
    private readonly IExpensesRepository _expensesRepository;
    private readonly IContainerWorkExpansionService _expansionService;
    private readonly ISelectedGroupContextResolver _groupContextResolver;
    private readonly IShiftRepository _shiftRepository;
    private readonly IDayLockService _dayLockService;

    public PostCommandHandler(
        IWorkRepository workRepository,
        ScheduleMapper scheduleMapper,
        IPeriodHoursService periodHoursService,
        IScheduleEntriesService scheduleEntriesService,
        IScheduleCompletionService completionService,
        IWorkNotificationFacade notificationFacade,
        IShiftExpensesRepository shiftExpensesRepository,
        IExpensesRepository expensesRepository,
        IContainerWorkExpansionService expansionService,
        ISelectedGroupContextResolver groupContextResolver,
        IShiftRepository shiftRepository,
        IDayLockService dayLockService,
        ILogger<PostCommandHandler> logger)
        : base(logger)
    {
        _workRepository = workRepository;
        _scheduleMapper = scheduleMapper;
        _periodHoursService = periodHoursService;
        _scheduleEntriesService = scheduleEntriesService;
        _completionService = completionService;
        _notificationFacade = notificationFacade;
        _shiftExpensesRepository = shiftExpensesRepository;
        _expensesRepository = expensesRepository;
        _expansionService = expansionService;
        _groupContextResolver = groupContextResolver;
        _shiftRepository = shiftRepository;
        _dayLockService = dayLockService;
    }

    public async Task<WorkResource?> Handle(PostCommand<WorkResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var work = _scheduleMapper.ToWorkEntity(request.Resource);

            await _dayLockService.EnsureNotLockedAsync(
                work.CurrentDate,
                work.ClientId,
                work.AnalyseToken,
                cancellationToken);

            await EnsureNoSporadicConflictAsync(work, cancellationToken);

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

    private async Task EnsureNoSporadicConflictAsync(Domain.Models.Schedules.Work work, CancellationToken cancellationToken)
    {
        var shift = await _shiftRepository.GetSporadicInfoAsync(work.ShiftId, cancellationToken);
        if (shift is null || !shift.IsSporadic)
        {
            return;
        }

        var (rangeFrom, rangeUntil) = SporadicRangeCalculator.Compute(shift, work.CurrentDate);

        var usage = await _workRepository.GetSporadicCapacityUsageAsync(
            work.ShiftId,
            work.CurrentDate,
            rangeFrom,
            rangeUntil,
            excludeWorkId: work.Id == Guid.Empty ? null : work.Id,
            work.AnalyseToken,
            cancellationToken);

        if (usage.EngagedAtDay >= shift.EffectiveSumEmployees)
        {
            throw new ConflictException(
                $"Sporadic shift '{shift.Name}' is fully booked on {work.CurrentDate:yyyy-MM-dd} " +
                $"({usage.EngagedAtDay}/{shift.EffectiveSumEmployees} employees).");
        }

        if (usage.EngagedAtDay == 0 && usage.DistinctBookedDays >= shift.EffectiveQuantity)
        {
            throw new ConflictException(
                $"Sporadic shift '{shift.Name}' has reached its range capacity " +
                $"({usage.DistinctBookedDays}/{shift.EffectiveQuantity} days) for scope {shift.SporadicScope} " +
                $"({rangeFrom:yyyy-MM-dd}..{rangeUntil:yyyy-MM-dd}).");
        }
    }
}
