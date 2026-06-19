// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Constants;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.DTOs.Schedules;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Application.Handlers.Expenses;

public class DeleteCommandHandler : BaseHandler, IRequestHandler<DeleteCommand<ExpensesResource>, ExpensesResource?>
{
    private readonly IExpensesRepository _expensesRepository;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPeriodHoursService _periodHoursService;
    private readonly IScheduleEntriesService _scheduleEntriesService;
    private readonly IWorkNotificationService _notificationService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IScheduleChangeTracker _scheduleChangeTracker;
    private readonly ISelectedGroupContextResolver _groupContextResolver;
    private readonly IDayLockService _dayLockService;

    public DeleteCommandHandler(
        IExpensesRepository expensesRepository,
        ScheduleMapper scheduleMapper,
        IUnitOfWork unitOfWork,
        IPeriodHoursService periodHoursService,
        IScheduleEntriesService scheduleEntriesService,
        IWorkNotificationService notificationService,
        IHttpContextAccessor httpContextAccessor,
        IScheduleChangeTracker scheduleChangeTracker,
        ISelectedGroupContextResolver groupContextResolver,
        IDayLockService dayLockService,
        ILogger<DeleteCommandHandler> logger)
        : base(logger)
    {
        _expensesRepository = expensesRepository;
        _scheduleMapper = scheduleMapper;
        _unitOfWork = unitOfWork;
        _periodHoursService = periodHoursService;
        _scheduleEntriesService = scheduleEntriesService;
        _notificationService = notificationService;
        _httpContextAccessor = httpContextAccessor;
        _scheduleChangeTracker = scheduleChangeTracker;
        _groupContextResolver = groupContextResolver;
        _dayLockService = dayLockService;
    }

    public async Task<ExpensesResource?> Handle(DeleteCommand<ExpensesResource> request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting Expenses with ID: {Id}", request.Id);

        var existingExpenses = await _expensesRepository.Get(request.Id);
        if (existingExpenses == null)
        {
            _logger.LogWarning("Expenses not found: {Id}", request.Id);
            return null;
        }

        var work = existingExpenses.Work;
        var expensesResource = _scheduleMapper.ToExpensesResource(existingExpenses);

        if (work != null)
        {
            await _dayLockService.EnsureNotLockedAsync(
                work.CurrentDate,
                work.ClientId,
                existingExpenses.AnalyseToken,
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

        _logger.LogInformation("Expenses deleted successfully: {Id}", request.Id);
        return expensesResource;
    }
}
