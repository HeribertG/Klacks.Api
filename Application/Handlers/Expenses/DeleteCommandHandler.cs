// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Deletes an expense entry and refreshes the schedule of its owner. An expense whose parent Work is owned by
/// a client outside the caller's group visibility is answered exactly like an expense that does not exist;
/// nothing is deleted. Scenario expenses (AnalyseToken set) are reachable too. A sealed parent Work refuses the
/// delete as decided by IParentWorkLockGuard.
/// </summary>
/// <param name="rowAccessGuard">Decides whether the calling user may see the touched clients and the scenario of the parent Work</param>
/// <param name="parentWorkLockGuard">Refuses the delete when the parent Work's lock level forbids it for the caller</param>

using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Constants;
using Klacks.Api.Application.Helpers;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.Handlers.Expenses;

public class DeleteCommandHandler : BaseHandler, IRequestHandler<DeleteCommand<ExpensesResource>, ExpensesResource?>
{
    private readonly IExpensesRepository _expensesRepository;
    private readonly IScheduleRowAccessGuard _rowAccessGuard;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPeriodHoursService _periodHoursService;
    private readonly IScheduleEntriesService _scheduleEntriesService;
    private readonly IWorkNotificationService _notificationService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IScheduleChangeTracker _scheduleChangeTracker;
    private readonly ISelectedGroupContextResolver _groupContextResolver;
    private readonly IDayLockService _dayLockService;
    private readonly IParentWorkLockGuard _parentWorkLockGuard;

    public DeleteCommandHandler(
        IExpensesRepository expensesRepository,
        IScheduleRowAccessGuard rowAccessGuard,
        ScheduleMapper scheduleMapper,
        IUnitOfWork unitOfWork,
        IPeriodHoursService periodHoursService,
        IScheduleEntriesService scheduleEntriesService,
        IWorkNotificationService notificationService,
        IHttpContextAccessor httpContextAccessor,
        IScheduleChangeTracker scheduleChangeTracker,
        ISelectedGroupContextResolver groupContextResolver,
        IDayLockService dayLockService,
        IParentWorkLockGuard parentWorkLockGuard,
        ILogger<DeleteCommandHandler> logger)
        : base(logger)
    {
        _expensesRepository = expensesRepository;
        _rowAccessGuard = rowAccessGuard;
        _scheduleMapper = scheduleMapper;
        _unitOfWork = unitOfWork;
        _periodHoursService = periodHoursService;
        _scheduleEntriesService = scheduleEntriesService;
        _notificationService = notificationService;
        _httpContextAccessor = httpContextAccessor;
        _scheduleChangeTracker = scheduleChangeTracker;
        _groupContextResolver = groupContextResolver;
        _dayLockService = dayLockService;
        _parentWorkLockGuard = parentWorkLockGuard;
    }

    public async Task<ExpensesResource?> Handle(DeleteCommand<ExpensesResource> request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting Expenses with ID: {Id}", request.Id);

        var existingExpenses = await _expensesRepository.GetWithWorkInAnyScope(request.Id);
        if (existingExpenses == null)
        {
            _logger.LogWarning("Expenses not found: {Id}", request.Id);
            return null;
        }

        var work = existingExpenses.Work;
        var visible = work != null
            ? await _rowAccessGuard.IsVisibleAsync(work.ClientId, work.AnalyseToken, cancellationToken)
            : await _rowAccessGuard.AreAllVisibleAsync([], existingExpenses.AnalyseToken, cancellationToken);
        if (!visible)
        {
            return null;
        }

        var expensesResource = _scheduleMapper.ToExpensesResource(existingExpenses);

        if (work != null)
        {
            _parentWorkLockGuard.EnsureChildWritableForCaller(work, _httpContextAccessor);

            await _dayLockService.EnsureNotLockedAsync(
                work.CurrentDate,
                work.ClientId,
                work.AnalyseToken,
                cancellationToken);
        }

        await _expensesRepository.Delete(request.Id);
        await _unitOfWork.CompleteAsync();

        if (work != null)
        {
            await _scheduleChangeTracker.TrackChangeAsync(work.ClientId, work.CurrentDate, work.AnalyseToken);
            var (periodStart, periodEnd) = await _periodHoursService.GetPeriodBoundariesAsync(work.CurrentDate);
            var connectionId = _httpContextAccessor.HttpContext?.Request
                .Headers[HttpHeaderNames.SignalRConnectionId].FirstOrDefault() ?? string.Empty;
            var notification = _scheduleMapper.ToScheduleNotificationDto(
                work.ClientId, work.CurrentDate, ScheduleEventTypes.Updated, connectionId, periodStart, periodEnd, work.AnalyseToken);
            await _notificationService.NotifyScheduleUpdated(notification);
            await _periodHoursService.RecalculateAndNotifyAsync(work.ClientId, periodStart, periodEnd, work.AnalyseToken, connectionId);

            var visibleGroupIds = await _groupContextResolver.ResolveVisibleGroupIdsAsync();
            var scheduleEntries = await _scheduleEntriesService.GetScheduleEntriesAsync(
                ScheduleEntriesCriteria.RefreshWindowAround(
                    work.CurrentDate, [work.ClientId], visibleGroupIds, work.AnalyseToken),
                cancellationToken);

            expensesResource.ScheduleEntries = scheduleEntries
                .Select(_scheduleMapper.ToWorkScheduleResource)
                .ToList();
        }

        _logger.LogInformation("Expenses deleted successfully: {Id}", request.Id);
        return expensesResource;
    }
}
