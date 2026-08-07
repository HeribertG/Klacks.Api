// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Seals a period (optionally scoped to a group) and raises a post-commit PeriodClosedEvent for country-pack hooks.
/// </summary>

using Klacks.Api.Application.Commands.PeriodClosing;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.PeriodClosing;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Events;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using System.Security.Claims;

namespace Klacks.Api.Application.Handlers.PeriodClosing;

/// <summary>
/// Handler for sealing a period by group, or the entire period when no group is specified.
/// </summary>
public class ClosePeriodByGroupCommandHandler : BaseTransactionHandler, IRequestHandler<ClosePeriodByGroupCommand, int>
{
    private readonly IWorkRepository _workRepository;
    private readonly IBreakRepository _breakRepository;
    private readonly IWorkLockLevelService _lockLevelService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IPeriodAuditLogRepository _auditLogRepository;
    private readonly ISealedDayRepository _sealedDayRepository;
    private readonly IDomainEventDispatcher _eventDispatcher;
    private readonly IPeriodValidationLoader _validationLoader;
    private readonly IComplianceEscalationService _escalationService;

    public ClosePeriodByGroupCommandHandler(
        IWorkRepository workRepository,
        IBreakRepository breakRepository,
        IWorkLockLevelService lockLevelService,
        IHttpContextAccessor httpContextAccessor,
        IPeriodAuditLogRepository auditLogRepository,
        ISealedDayRepository sealedDayRepository,
        IDomainEventDispatcher eventDispatcher,
        IPeriodValidationLoader validationLoader,
        IComplianceEscalationService escalationService,
        IUnitOfWork unitOfWork,
        ILogger<ClosePeriodByGroupCommandHandler> logger)
        : base(unitOfWork, logger)
    {
        _workRepository = workRepository;
        _breakRepository = breakRepository;
        _lockLevelService = lockLevelService;
        _httpContextAccessor = httpContextAccessor;
        _auditLogRepository = auditLogRepository;
        _sealedDayRepository = sealedDayRepository;
        _eventDispatcher = eventDispatcher;
        _validationLoader = validationLoader;
        _escalationService = escalationService;
    }

    /// <summary>
    /// Validates the request, checks permissions, performs the seal operation, and writes the audit log entry.
    /// </summary>
    /// <param name="request">Contains StartDate, EndDate, optional GroupId and optional Reason</param>
    public async Task<int> Handle(ClosePeriodByGroupCommand request, CancellationToken cancellationToken)
    {
        var capturedSealedBy = "Unknown";
        var capturedWorkCount = 0;
        var capturedBreakCount = 0;
        var capturedSealedDayCount = 0;

        var total = await ExecuteWithTransactionAsync(async () =>
        {
            if (request.StartDate > request.EndDate)
                throw new Domain.Exceptions.InvalidRequestException("Start date must be before or equal to end date.");

            var isAdmin = _httpContextAccessor.HttpContext?.User?.IsInRole(Roles.Admin) == true;
            var isAuthorised = _httpContextAccessor.HttpContext?.User?.IsInRole(Roles.Authorised) == true;

            if (!_lockLevelService.CanSeal(WorkLockLevel.None, WorkLockLevel.Closed, isAdmin, isAuthorised))
                throw new Domain.Exceptions.InvalidRequestException("You do not have permission to close periods.");

            await EnsureViolationsAcknowledgedAsync(request, cancellationToken);

            var sealedBy = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Unknown";

            int workCount;
            int breakCount;

            if (request.GroupId.HasValue)
            {
                workCount = await _workRepository.SealByPeriodAndGroup(request.StartDate, request.EndDate, request.GroupId.Value, WorkLockLevel.Closed, sealedBy, cancellationToken);
                breakCount = await _breakRepository.SealByPeriodAndGroup(request.StartDate, request.EndDate, request.GroupId.Value, WorkLockLevel.Closed, sealedBy, cancellationToken);
            }
            else
            {
                workCount = await _workRepository.SealByPeriod(request.StartDate, request.EndDate, WorkLockLevel.Closed, sealedBy, cancellationToken);
                breakCount = await _breakRepository.SealByPeriod(request.StartDate, request.EndDate, WorkLockLevel.Closed, sealedBy, cancellationToken);
            }

            var existingSealedDays = await _sealedDayRepository.GetRangeAsync(
                request.StartDate, request.EndDate, request.GroupId, cancellationToken);
            var existingDates = new HashSet<DateOnly>(existingSealedDays.Select(s => s.Date));

            var sealedDayCount = 0;
            for (var d = request.StartDate; d <= request.EndDate; d = d.AddDays(1))
            {
                if (existingDates.Contains(d))
                {
                    continue;
                }
                await _sealedDayRepository.AddAsync(new SealedDay
                {
                    Date = d,
                    GroupId = request.GroupId,
                    Level = WorkLockLevel.Closed,
                    Reason = request.Reason,
                    SealedAt = DateTime.UtcNow,
                    SealedBy = sealedBy
                }, cancellationToken);
                sealedDayCount++;
            }

            var affected = workCount + breakCount + sealedDayCount;

            await _auditLogRepository.AddAsync(new PeriodAuditLog
            {
                Action = PeriodAuditAction.Seal,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                GroupId = request.GroupId,
                Reason = request.Reason,
                AffectedCount = affected,
                PerformedAt = DateTime.UtcNow,
                PerformedBy = sealedBy
            }, cancellationToken);

            capturedSealedBy = sealedBy;
            capturedWorkCount = workCount;
            capturedBreakCount = breakCount;
            capturedSealedDayCount = sealedDayCount;

            return affected;
        },
        "closing period (group-aware)",
        new { request.StartDate, request.EndDate, request.GroupId });

        await DispatchPeriodClosedAsync(request, capturedSealedBy, capturedWorkCount, capturedBreakCount, capturedSealedDayCount);

        return total;
    }

    /// <summary>
    /// Dispatches the PeriodClosedEvent after the seal transaction has committed. Runs non-blocking with an
    /// uncancellable token: a failing hook is logged and never affects the already-committed seal or the result.
    /// </summary>
    private async Task DispatchPeriodClosedAsync(
        ClosePeriodByGroupCommand request,
        string sealedBy,
        int workCount,
        int breakCount,
        int sealedDayCount)
    {
        try
        {
            var domainEvent = new PeriodClosedEvent(
                request.StartDate,
                request.EndDate,
                request.GroupId,
                workCount,
                breakCount,
                sealedDayCount,
                sealedBy);

            await _eventDispatcher.DispatchAsync(domainEvent, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Post-commit dispatch of {EventName} failed for period {Start}..{End}; the seal is committed and remains unaffected.",
                nameof(PeriodClosedEvent),
                request.StartDate,
                request.EndDate);
        }
    }

    /// <summary>
    /// Refuses to seal a period that still holds errors until the caller confirms having seen them.
    /// Sealing is deliberately not forbidden - a period can be closed over a known violation - but it
    /// must be a decision, not a side effect: before this, a period sealed regardless of what the
    /// issues card was showing, and the seal is what makes those days unwritable.
    /// Warnings never gate the close; only errors do, including the ones a rule configured as Block
    /// escalates to error, which is why the findings run through the same escalation as the issues
    /// endpoint.
    /// A confirmation that names the count it was issued for is re-checked against the current state:
    /// errors that appeared between the refusal and the confirmation would otherwise be sealed over
    /// unseen, because the confirmation is a snapshot decision. A confirmation without a count keeps
    /// the legacy behaviour and seals without the re-check.
    /// </summary>
    private async Task EnsureViolationsAcknowledgedAsync(
        ClosePeriodByGroupCommand request,
        CancellationToken cancellationToken)
    {
        var acknowledgedErrorCount = request.AcknowledgeViolations ? request.AcknowledgedErrorCount : null;
        if (request.AcknowledgeViolations && acknowledgedErrorCount is null)
        {
            return;
        }

        var issues = await _validationLoader.LoadAsync(
            request.StartDate, request.EndDate, request.GroupId, cancellationToken: cancellationToken);
        await _escalationService.EscalateBlockedIssuesAsync(issues);

        var errorCount = issues.Count(i => i.Severity == ScheduleValidationType.Error);
        if (errorCount == 0 || errorCount <= acknowledgedErrorCount)
        {
            return;
        }

        var advice = request.AcknowledgeViolations
            ? "That is more than the acknowledged count, so new findings appeared since the confirmation. "
              + "Review them and confirm again."
            : "Review them and repeat the request with acknowledgeViolations set to seal the period anyway.";

        throw new Klacks.Api.Application.Exceptions.PeriodValidationConflictException(
            $"The period {request.StartDate:yyyy-MM-dd}..{request.EndDate:yyyy-MM-dd} still holds " +
            $"{errorCount} unresolved error(s). {advice}",
            errorCount);
    }
}
