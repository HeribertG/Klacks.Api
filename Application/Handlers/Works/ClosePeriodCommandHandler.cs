// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Seals the whole period (work and break entries) and raises a post-commit PeriodClosedEvent for country-pack hooks.
/// Runs inside a transaction for the same reason the group-aware close does: the acknowledgement check
/// loads the period's findings, and that load deliberately reconciles the materialised K12 state before
/// reading it. Without the transaction a REFUSED close would still leave those reconcile writes behind.
/// </summary>

using Klacks.Api.Application.Commands.Works;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.PeriodClosing;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Events;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Infrastructure.Mediator;
using System.Security.Claims;

namespace Klacks.Api.Application.Handlers.Works;

public class ClosePeriodCommandHandler : BaseTransactionHandler, IRequestHandler<ClosePeriodCommand, int>
{
    private readonly IWorkRepository _workRepository;
    private readonly IBreakRepository _breakRepository;
    private readonly IWorkLockLevelService _lockLevelService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IDomainEventDispatcher _eventDispatcher;
    private readonly IPeriodValidationLoader _validationLoader;
    private readonly IComplianceEscalationService _escalationService;

    public ClosePeriodCommandHandler(
        IWorkRepository workRepository,
        IBreakRepository breakRepository,
        IWorkLockLevelService lockLevelService,
        IHttpContextAccessor httpContextAccessor,
        IDomainEventDispatcher eventDispatcher,
        IPeriodValidationLoader validationLoader,
        IComplianceEscalationService escalationService,
        IUnitOfWork unitOfWork,
        ILogger<ClosePeriodCommandHandler> logger)
        : base(unitOfWork, logger)
    {
        _workRepository = workRepository;
        _breakRepository = breakRepository;
        _lockLevelService = lockLevelService;
        _httpContextAccessor = httpContextAccessor;
        _eventDispatcher = eventDispatcher;
        _validationLoader = validationLoader;
        _escalationService = escalationService;
    }

    public async Task<int> Handle(ClosePeriodCommand request, CancellationToken cancellationToken)
    {
        var capturedSealedBy = "Unknown";
        var capturedWorkCount = 0;
        var capturedBreakCount = 0;

        var total = await ExecuteWithTransactionAsync(async () =>
        {
            var isAdmin = _httpContextAccessor.HttpContext?.User?.IsInRole(Roles.Admin) == true;
            var isAuthorised = _httpContextAccessor.HttpContext?.User?.IsInRole(Roles.Authorised) == true;

            if (!_lockLevelService.CanSeal(WorkLockLevel.None, WorkLockLevel.Closed, isAdmin, isAuthorised))
                throw new Domain.Exceptions.InvalidRequestException("You do not have permission to close periods.");

            await EnsureViolationsAcknowledgedAsync(request, cancellationToken);

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
    /// Dispatches the PeriodClosedEvent after the seal transaction has committed. Runs non-blocking with an
    /// uncancellable token: a failing hook is logged and never affects the already-committed seal or the result.
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
                "Post-commit dispatch of {EventName} failed for period {Start}..{End}; the seal is committed and remains unaffected.",
                nameof(PeriodClosedEvent),
                request.StartDate,
                request.EndDate);
        }
    }

    /// <summary>
    /// Refuses to seal a period that still holds errors until the caller confirms having seen them.
    /// Same contract as the group-aware close: sealing over a known violation stays possible, sealing
    /// without knowing does not. Warnings never gate; errors do, including those a Block-mode rule
    /// escalates, hence the same escalation the issues endpoint applies.
    /// A confirmation that names the count it was issued for is re-checked against the current state:
    /// errors that appeared between the refusal and the confirmation would otherwise be sealed over
    /// unseen, because the confirmation is a snapshot decision. A confirmation without a count keeps
    /// the legacy behaviour and seals without the re-check.
    /// Must stay INSIDE the handler's transaction: the load reconciles the materialised K12 state before
    /// reading it, so a refusal raised outside the transaction would persist those writes on an operation
    /// the caller never carried out.
    /// </summary>
    private async Task EnsureViolationsAcknowledgedAsync(
        ClosePeriodCommand request,
        CancellationToken cancellationToken)
    {
        var acknowledgedErrorCount = request.AcknowledgeViolations ? request.AcknowledgedErrorCount : null;
        if (request.AcknowledgeViolations && acknowledgedErrorCount is null)
        {
            return;
        }

        var issues = await _validationLoader.LoadAsync(
            request.StartDate, request.EndDate, null, cancellationToken: cancellationToken);
        await _escalationService.EscalateBlockedIssuesAsync(issues);

        var errorCount = issues.Count(i => i.Severity == Domain.Enums.ScheduleValidationType.Error);
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
