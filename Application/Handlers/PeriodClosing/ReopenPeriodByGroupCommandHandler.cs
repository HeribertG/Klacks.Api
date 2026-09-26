// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Commands.PeriodClosing;
using Klacks.Api.Application.DTOs.PeriodClosing;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using System.Security.Claims;

namespace Klacks.Api.Application.Handlers.PeriodClosing;

/// <summary>
/// Handler for unsealing a period by group, or the entire period when no group is specified.
/// A non-empty reason is mandatory for every reopen operation. Each reopened work and break entry goes back to
/// the lock level it had before the period seal (Confirmed and Approved survive a close/reopen); entries sealed
/// before that level was recorded reopen to None. The result reports which is which.
/// </summary>
public class ReopenPeriodByGroupCommandHandler : BaseTransactionHandler, IRequestHandler<ReopenPeriodByGroupCommand, PeriodReopenResult>
{
    private readonly IWorkRepository _workRepository;
    private readonly IBreakRepository _breakRepository;
    private readonly IWorkLockLevelService _lockLevelService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IPeriodAuditLogRepository _auditLogRepository;
    private readonly ISealedDayRepository _sealedDayRepository;
    private readonly IUserService _userService;

    public ReopenPeriodByGroupCommandHandler(
        IWorkRepository workRepository,
        IBreakRepository breakRepository,
        IWorkLockLevelService lockLevelService,
        IHttpContextAccessor httpContextAccessor,
        IPeriodAuditLogRepository auditLogRepository,
        ISealedDayRepository sealedDayRepository,
        IUserService userService,
        IUnitOfWork unitOfWork,
        ILogger<ReopenPeriodByGroupCommandHandler> logger)
        : base(unitOfWork, logger)
    {
        _workRepository = workRepository;
        _breakRepository = breakRepository;
        _lockLevelService = lockLevelService;
        _httpContextAccessor = httpContextAccessor;
        _auditLogRepository = auditLogRepository;
        _sealedDayRepository = sealedDayRepository;
        _userService = userService;
    }

    /// <summary>
    /// Validates the request, checks permissions, performs the unseal operation, and writes the audit log entry.
    /// </summary>
    /// <param name="request">Contains StartDate, EndDate, optional GroupId and mandatory Reason</param>
    public async Task<PeriodReopenResult> Handle(ReopenPeriodByGroupCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithTransactionAsync(async () =>
        {
            if (string.IsNullOrWhiteSpace(request.Reason))
                throw new Domain.Exceptions.InvalidRequestException("A reason must be provided when reopening a period.");

            if (request.StartDate > request.EndDate)
                throw new Domain.Exceptions.InvalidRequestException("Start date must be before or equal to end date.");

            var isAdmin = _httpContextAccessor.HttpContext?.User?.IsInRole(Roles.Admin) == true;
            var isAuthorised = _httpContextAccessor.HttpContext?.User?.IsInRole(Roles.Authorised) == true;

            if (!_lockLevelService.CanUnseal(WorkLockLevel.Closed, isAdmin, isAuthorised))
                throw new Domain.Exceptions.InvalidRequestException("You do not have permission to reopen periods.");

            var userName = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? AuditActorDefaults.UnknownActor;

            PeriodUnsealCounts workCounts;
            PeriodUnsealCounts breakCounts;

            if (request.GroupId.HasValue)
            {
                workCounts = await _workRepository.UnsealByPeriodAndGroup(request.StartDate, request.EndDate, request.GroupId.Value, WorkLockLevel.Closed, cancellationToken);
                breakCounts = await _breakRepository.UnsealByPeriodAndGroup(request.StartDate, request.EndDate, request.GroupId.Value, WorkLockLevel.Closed, cancellationToken);
            }
            else
            {
                workCounts = await _workRepository.UnsealByPeriod(request.StartDate, request.EndDate, WorkLockLevel.Closed, cancellationToken);
                breakCounts = await _breakRepository.UnsealByPeriod(request.StartDate, request.EndDate, WorkLockLevel.Closed, cancellationToken);
            }

            var sealedDayCount = await _sealedDayRepository.SoftDeleteRangeAsync(
                request.StartDate, request.EndDate, request.GroupId, userName, cancellationToken);

            var entries = workCounts + breakCounts;
            var total = entries.Total + sealedDayCount;

            _logger.LogInformation(
                "Reopened period {Start}..{End} (group {GroupId}): {Confirmed} entries back to Confirmed, {Approved} to Approved, " +
                "{None} to None, {WithoutRecord} without recorded pre-seal level reopened to None, {SealedDays} day locks lifted",
                request.StartDate, request.EndDate, request.GroupId, entries.RestoredConfirmed, entries.RestoredApproved,
                entries.RestoredNone, entries.WithoutRecordedLevel, sealedDayCount);

            await _auditLogRepository.AddAsync(
                PeriodAuditLog.For(
                    PeriodAuditAction.Unseal,
                    request.StartDate,
                    request.EndDate,
                    request.GroupId,
                    request.Reason.Trim(),
                    total,
                    userName,
                    _userService.GetDisplayName()),
                cancellationToken);

            return new PeriodReopenResult(total, sealedDayCount, entries);
        },
        "reopening period (group-aware)",
        new { request.StartDate, request.EndDate, request.GroupId });
    }
}
