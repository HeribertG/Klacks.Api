// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Creates an expense entry on a Work and refreshes the schedule of its owner. An expense on a Work owned by
/// a client outside the caller's group visibility is refused exactly like an expense on a missing or deleted
/// Work (KeyNotFoundException, 404), without revealing the owner; nothing is written. The expense always belongs to the scope (main plan or scenario) of its parent
/// Work: the AnalyseToken is taken from the Work, never from the request. A sealed parent Work refuses the write
/// as decided by IParentWorkLockGuard.
/// </summary>
/// <param name="clientVisibilityGuard">Decides whether the calling user may write for the owning client</param>
/// <param name="parentWorkLockGuard">Refuses the write when the parent Work's lock level forbids it for the caller</param>

using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Constants;
using Klacks.Api.Application.Helpers;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.DTOs.Schedules;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Application.Handlers.Expenses;

public class PostCommandHandler : BaseHandler, IRequestHandler<PostCommand<ExpensesResource>, ExpensesResource?>
{
    private readonly IExpensesRepository _expensesRepository;
    private readonly IClientVisibilityGuard _clientVisibilityGuard;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPeriodHoursService _periodHoursService;
    private readonly IScheduleEntriesService _scheduleEntriesService;
    private readonly IWorkNotificationService _notificationService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IScheduleChangeTracker _scheduleChangeTracker;
    private readonly ISelectedGroupContextResolver _groupContextResolver;
    private readonly IWorkRepository _workRepository;
    private readonly IDayLockService _dayLockService;
    private readonly IParentWorkLockGuard _parentWorkLockGuard;

    public PostCommandHandler(
        IExpensesRepository expensesRepository,
        IClientVisibilityGuard clientVisibilityGuard,
        ScheduleMapper scheduleMapper,
        IUnitOfWork unitOfWork,
        IPeriodHoursService periodHoursService,
        IScheduleEntriesService scheduleEntriesService,
        IWorkNotificationService notificationService,
        IHttpContextAccessor httpContextAccessor,
        IScheduleChangeTracker scheduleChangeTracker,
        ISelectedGroupContextResolver groupContextResolver,
        IWorkRepository workRepository,
        IDayLockService dayLockService,
        IParentWorkLockGuard parentWorkLockGuard,
        ILogger<PostCommandHandler> logger)
        : base(logger)
    {
        _expensesRepository = expensesRepository;
        _clientVisibilityGuard = clientVisibilityGuard;
        _scheduleMapper = scheduleMapper;
        _unitOfWork = unitOfWork;
        _periodHoursService = periodHoursService;
        _scheduleEntriesService = scheduleEntriesService;
        _notificationService = notificationService;
        _httpContextAccessor = httpContextAccessor;
        _scheduleChangeTracker = scheduleChangeTracker;
        _groupContextResolver = groupContextResolver;
        _workRepository = workRepository;
        _dayLockService = dayLockService;
        _parentWorkLockGuard = parentWorkLockGuard;
    }

    public async Task<ExpensesResource?> Handle(PostCommand<ExpensesResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var expenses = _scheduleMapper.ToExpensesEntity(request.Resource);

            var parentWork = await _workRepository.GetNoTracking(expenses.WorkId);
            if (parentWork == null || !await _clientVisibilityGuard.IsVisibleAsync(parentWork.ClientId, cancellationToken))
            {
                throw new KeyNotFoundException($"Work with ID {expenses.WorkId} not found");
            }

            expenses.AnalyseToken = parentWork.AnalyseToken;

            _parentWorkLockGuard.EnsureChildWritableForCaller(parentWork, _httpContextAccessor);

            await _dayLockService.EnsureNotLockedAsync(
                parentWork.CurrentDate,
                parentWork.ClientId,
                parentWork.AnalyseToken,
                cancellationToken);

            await _expensesRepository.Add(expenses);
            await _unitOfWork.CompleteAsync();

            var expensesResource = _scheduleMapper.ToExpensesResource(expenses);

            var expensesWithWork = await _expensesRepository.GetWithWorkInAnyScope(expenses.Id);
            if (expensesWithWork?.Work != null)
            {
                var work = expensesWithWork.Work;
                await _scheduleChangeTracker.TrackChangeAsync(work.ClientId, work.CurrentDate, work.AnalyseToken);
                var (periodStart, periodEnd) = await _periodHoursService.GetPeriodBoundariesAsync(work.CurrentDate);
                var connectionId = _httpContextAccessor.HttpContext?.Request
                    .Headers[HttpHeaderNames.SignalRConnectionId].FirstOrDefault() ?? string.Empty;

                var notification = _scheduleMapper.ToScheduleNotificationDto(
                    work.ClientId, work.CurrentDate, ScheduleEventTypes.Updated, connectionId, periodStart, periodEnd, work.AnalyseToken);
                await _notificationService.NotifyScheduleUpdated(notification);

                await _periodHoursService.RecalculateAndNotifyAsync(work.ClientId, periodStart, periodEnd, work.AnalyseToken, connectionId);

                var threeDayStart = work.CurrentDate.AddDays(-1);
                var threeDayEnd = work.CurrentDate.AddDays(1);
                var visibleGroupIds = await _groupContextResolver.ResolveVisibleGroupIdsAsync();
                var scheduleEntries = await _scheduleEntriesService
                    .GetScheduleEntriesQuery(threeDayStart, threeDayEnd, visibleGroupIds, work.AnalyseToken)
                    .Where(e => e.ClientId == work.ClientId)
                    .ToListAsync(cancellationToken);

                expensesResource.ScheduleEntries = scheduleEntries
                    .Select(_scheduleMapper.ToWorkScheduleResource)
                    .ToList();
            }

            return expensesResource;
        }, "CreateExpenses", new { request.Resource.Id });
    }
}
