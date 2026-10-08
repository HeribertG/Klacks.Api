// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Updates a single break (absence entry) and notifies the schedule. A break whose stored owner or whose
/// new owner is outside the caller's group visibility is refused exactly like a break that does not exist;
/// nothing is written. A break sealed by a period close (LockLevel Closed) may only be changed by an admin - the
/// same rule the delete validator applies; independently of the role, the day lock refuses any write on a day that
/// is sealed for the client (globally, or by a group the client is a member of or worked for that day). The
/// AnalyseToken is taken from the stored row, never from the payload, so a PUT cannot flip a main-plan break into a
/// scenario and bypass the day lock.
/// </summary>
/// <param name="clientVisibilityGuard">Decides whether the calling user may write for the client</param>

using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Constants;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Macros;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Services.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.DTOs.Schedules;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Application.Handlers.Breaks;

public class PutCommandHandler : BaseHandler, IRequestHandler<PutCommand<BreakResource>, BreakResource?>
{
    private readonly IBreakRepository _breakRepository;
    private readonly IClientVisibilityGuard _clientVisibilityGuard;
    private readonly IBreakMacroService _breakMacroService;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IPeriodHoursService _periodHoursService;
    private readonly IScheduleEntriesService _scheduleEntriesService;
    private readonly IWorkNotificationService _notificationService;
    private readonly IScheduleCompletionService _completionService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ISelectedGroupContextResolver _groupContextResolver;
    private readonly IDayLockService _dayLockService;

    public PutCommandHandler(
        IBreakRepository breakRepository,
        IClientVisibilityGuard clientVisibilityGuard,
        IBreakMacroService breakMacroService,
        ScheduleMapper scheduleMapper,
        IPeriodHoursService periodHoursService,
        IScheduleEntriesService scheduleEntriesService,
        IWorkNotificationService notificationService,
        IScheduleCompletionService completionService,
        IHttpContextAccessor httpContextAccessor,
        ISelectedGroupContextResolver groupContextResolver,
        IDayLockService dayLockService,
        ILogger<PutCommandHandler> logger)
        : base(logger)
    {
        _breakRepository = breakRepository;
        _clientVisibilityGuard = clientVisibilityGuard;
        _breakMacroService = breakMacroService;
        _scheduleMapper = scheduleMapper;
        _periodHoursService = periodHoursService;
        _scheduleEntriesService = scheduleEntriesService;
        _notificationService = notificationService;
        _completionService = completionService;
        _httpContextAccessor = httpContextAccessor;
        _groupContextResolver = groupContextResolver;
        _dayLockService = dayLockService;
    }

    public async Task<BreakResource?> Handle(PutCommand<BreakResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var existing = await _breakRepository.GetNoTracking(request.Resource.Id);
            var entity = _scheduleMapper.ToBreakEntity(request.Resource);
            ScheduleEntrySealState.CarryOver(entity, existing);
            entity.AnalyseToken = existing?.AnalyseToken;

            var ownerIds = existing != null
                ? new[] { existing.ClientId, entity.ClientId }
                : new[] { entity.ClientId };
            if (!await _clientVisibilityGuard.AreAllVisibleAsync(ownerIds, cancellationToken))
            {
                throw new KeyNotFoundException($"Break with ID {request.Resource.Id} not found");
            }

            if (existing?.LockLevel == WorkLockLevel.Closed
                && _httpContextAccessor.HttpContext?.User?.IsInRole(Roles.Admin) != true)
            {
                throw new InvalidRequestException("Cannot modify a closed break entry.");
            }

            if (existing != null)
            {
                await _dayLockService.EnsureNotLockedAsync(
                    existing.CurrentDate,
                    existing.ClientId,
                    entity.AnalyseToken,
                    cancellationToken);
            }

            await _dayLockService.EnsureNotLockedAsync(
                entity.CurrentDate,
                entity.ClientId,
                entity.AnalyseToken,
                cancellationToken);

            var (periodStart, periodEnd) = await _periodHoursService.GetPeriodBoundariesAsync(entity.CurrentDate);

            await _breakMacroService.ProcessBreakMacroAsync(entity, request.Resource.PaymentInterval);
            var updated = await _breakRepository.Put(entity);

            if (updated == null)
            {
                throw new KeyNotFoundException($"Break with ID {request.Resource.Id} not found");
            }

            var periodHours = await _completionService.SaveAndTrackAsync(
                updated.ClientId, updated.CurrentDate, periodStart, periodEnd, updated.AnalyseToken);

            var currentDate = updated.CurrentDate;
            var threeDayStart = currentDate.AddDays(-1);
            var threeDayEnd = currentDate.AddDays(1);

            var visibleGroupIds = await _groupContextResolver.ResolveVisibleGroupIdsAsync();
            var scheduleEntries = await _scheduleEntriesService
                .GetScheduleEntriesQuery(threeDayStart, threeDayEnd, visibleGroupIds, updated.AnalyseToken)
                .Where(e => e.ClientId == updated.ClientId)
                .ToListAsync(cancellationToken);

            var breakResource = _scheduleMapper.ToBreakResource(updated);
            breakResource.PeriodHours = periodHours;
            breakResource.ScheduleEntries = scheduleEntries.Select(_scheduleMapper.ToWorkScheduleResource).ToList();

            var connectionId = _httpContextAccessor.HttpContext?.Request
                .Headers[HttpHeaderNames.SignalRConnectionId].FirstOrDefault() ?? string.Empty;
            var notification = _scheduleMapper.ToScheduleNotificationDto(
                updated.ClientId, updated.CurrentDate, ScheduleEventTypes.Updated, connectionId, periodStart, periodEnd, updated.AnalyseToken);
            await _notificationService.NotifyScheduleUpdated(notification);

            return breakResource;
        }, "UpdateBreak", new { request.Resource.Id });
    }
}
