// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Seals the whole period (work and break entries) and raises a post-commit PeriodClosedEvent for country-pack hooks.
/// </summary>

using Klacks.Api.Application.Commands.Works;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Events;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Infrastructure.Mediator;
using System.Security.Claims;

namespace Klacks.Api.Application.Handlers.Works;

public class ClosePeriodCommandHandler : BaseHandler, IRequestHandler<ClosePeriodCommand, int>
{
    private readonly IWorkRepository _workRepository;
    private readonly IBreakRepository _breakRepository;
    private readonly IWorkLockLevelService _lockLevelService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IDomainEventDispatcher _eventDispatcher;

    public ClosePeriodCommandHandler(
        IWorkRepository workRepository,
        IBreakRepository breakRepository,
        IWorkLockLevelService lockLevelService,
        IHttpContextAccessor httpContextAccessor,
        IDomainEventDispatcher eventDispatcher,
        ILogger<ClosePeriodCommandHandler> logger)
        : base(logger)
    {
        _workRepository = workRepository;
        _breakRepository = breakRepository;
        _lockLevelService = lockLevelService;
        _httpContextAccessor = httpContextAccessor;
        _eventDispatcher = eventDispatcher;
    }

    public async Task<int> Handle(ClosePeriodCommand request, CancellationToken cancellationToken)
    {
        var capturedSealedBy = "Unknown";
        var capturedWorkCount = 0;
        var capturedBreakCount = 0;

        var total = await ExecuteAsync(async () =>
        {
            var isAdmin = _httpContextAccessor.HttpContext?.User?.IsInRole(Roles.Admin) == true;
            var isAuthorised = _httpContextAccessor.HttpContext?.User?.IsInRole(Roles.Authorised) == true;

            if (!_lockLevelService.CanSeal(WorkLockLevel.None, WorkLockLevel.Closed, isAdmin, isAuthorised))
                throw new Domain.Exceptions.InvalidRequestException("You do not have permission to close periods.");

            var sealedBy = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Unknown";

            var workCount = await _workRepository.SealByPeriod(request.StartDate, request.EndDate, WorkLockLevel.Closed, sealedBy, cancellationToken);
            var breakCount = await _breakRepository.SealByPeriod(request.StartDate, request.EndDate, WorkLockLevel.Closed, sealedBy, cancellationToken);

            capturedSealedBy = sealedBy;
            capturedWorkCount = workCount;
            capturedBreakCount = breakCount;

            return workCount + breakCount;
        },
        "closing period",
        new { request.StartDate, request.EndDate });

        await DispatchPeriodClosedAsync(request, capturedSealedBy, capturedWorkCount, capturedBreakCount);

        return total;
    }

    /// <summary>
    /// Dispatches the PeriodClosedEvent after the seal has been persisted. Runs non-blocking with an
    /// uncancellable token: a failing hook is logged and never affects the seal or the returned result.
    /// This whole-period path carries no group scope, so GroupId is null and SealedDayCount is zero.
    /// </summary>
    private async Task DispatchPeriodClosedAsync(
        ClosePeriodCommand request,
        string sealedBy,
        int workCount,
        int breakCount)
    {
        try
        {
            var domainEvent = new PeriodClosedEvent(
                request.StartDate,
                request.EndDate,
                null,
                workCount,
                breakCount,
                0,
                sealedBy);

            await _eventDispatcher.DispatchAsync(domainEvent, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Post-commit dispatch of {EventName} failed for period {Start}..{End}; the seal is persisted and remains unaffected.",
                nameof(PeriodClosedEvent),
                request.StartDate,
                request.EndDate);
        }
    }
}
