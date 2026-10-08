// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Updates an expense entry and refreshes the schedule of its owner. An expense whose old or new parent Work
/// is owned by a client outside the caller's group visibility is answered exactly like an expense that does
/// not exist; nothing is written, and so is one whose old or new parent Work is missing or deleted. The AnalyseToken follows the parent Work (the request carries none), and an expense
/// cannot be moved between a scenario and the main plan. A sealed old or new parent Work refuses the write as
/// decided by IParentWorkLockGuard.
/// </summary>
/// <param name="clientVisibilityGuard">Decides whether the calling user may write for the owners of both parent Works</param>
/// <param name="parentWorkLockGuard">Refuses the write when a parent Work's lock level forbids it for the caller</param>

using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Constants;
using Klacks.Api.Application.Helpers;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.DTOs.Schedules;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Application.Handlers.Expenses;

public class PutCommandHandler : BaseHandler, IRequestHandler<PutCommand<ExpensesResource>, ExpensesResource?>
{
    public const string CrossScopeMoveMessage =
        "An expense cannot be moved between a scenario and the main plan.";

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

    public PutCommandHandler(
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
        ILogger<PutCommandHandler> logger)
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

    public async Task<ExpensesResource?> Handle(PutCommand<ExpensesResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            _logger.LogInformation("Updating Expenses with ID: {Id}", request.Resource.Id);

            var existingExpenses = await _expensesRepository.GetNoTracking(request.Resource.Id);
            if (existingExpenses == null)
            {
                _logger.LogWarning("Expenses not found: {Id}", request.Resource.Id);
                return null;
            }

            var expenses = _scheduleMapper.ToExpensesEntity(request.Resource);
            var isMoved = existingExpenses.WorkId != expenses.WorkId;
            var parentWork = await _workRepository.GetNoTracking(expenses.WorkId);
            var oldParentWork = isMoved
                ? await _workRepository.GetNoTracking(existingExpenses.WorkId)
                : parentWork;
            if (parentWork == null || oldParentWork == null)
            {
                _logger.LogWarning("Parent work of Expenses not found: {Id}", request.Resource.Id);
                return null;
            }

            var clientIds = new[] { parentWork.ClientId, oldParentWork.ClientId }.Distinct().ToList();
            if (!await _clientVisibilityGuard.AreAllVisibleAsync(clientIds, cancellationToken))
            {
                return null;
            }

            if (oldParentWork.AnalyseToken != parentWork.AnalyseToken)
            {
                throw new InvalidRequestException(CrossScopeMoveMessage);
            }

            expenses.AnalyseToken = parentWork.AnalyseToken;

            _parentWorkLockGuard.EnsureChildWritableForCaller(parentWork, _httpContextAccessor);
            if (isMoved)
            {
                _parentWorkLockGuard.EnsureChildWritableForCaller(oldParentWork, _httpContextAccessor);
            }

            await _dayLockService.EnsureNotLockedAsync(
                parentWork.CurrentDate,
                parentWork.ClientId,
                parentWork.AnalyseToken,
                cancellationToken);

            if (isMoved)
            {
                await _dayLockService.EnsureNotLockedAsync(
                    oldParentWork.CurrentDate,
                    oldParentWork.ClientId,
                    oldParentWork.AnalyseToken,
                    cancellationToken);
            }

            var updatedExpenses = await _expensesRepository.Put(expenses);
            await _unitOfWork.CompleteAsync();

            if (updatedExpenses == null)
            {
                return null;
            }

            var resultResource = _scheduleMapper.ToExpensesResource(updatedExpenses);

            var expensesWithWork = await _expensesRepository.GetWithWorkInAnyScope(updatedExpenses.Id);
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

                resultResource.ScheduleEntries = scheduleEntries
                    .Select(_scheduleMapper.ToWorkScheduleResource)
                    .ToList();
            }

            _logger.LogInformation("Expenses updated successfully: {Id}", request.Resource.Id);
            return resultResource;
        }, "UpdateExpenses", new { request.Resource.Id });
    }
}
