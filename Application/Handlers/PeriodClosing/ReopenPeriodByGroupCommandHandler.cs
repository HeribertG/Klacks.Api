// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for unsealing a period by group, or the entire period when no group is specified.
/// A non-empty reason is mandatory for every reopen operation.
/// </summary>
/// <param name="request">Contains StartDate, EndDate, optional GroupId and mandatory Reason</param>

using Klacks.Api.Application.Commands.PeriodClosing;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using System.Security.Claims;

namespace Klacks.Api.Application.Handlers.PeriodClosing;

public class ReopenPeriodByGroupCommandHandler : BaseTransactionHandler, IRequestHandler<ReopenPeriodByGroupCommand, int>
{
    private readonly IWorkRepository _workRepository;
    private readonly IBreakRepository _breakRepository;
    private readonly IWorkLockLevelService _lockLevelService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IPeriodAuditLogRepository _auditLogRepository;

    public ReopenPeriodByGroupCommandHandler(
        IWorkRepository workRepository,
        IBreakRepository breakRepository,
        IWorkLockLevelService lockLevelService,
        IHttpContextAccessor httpContextAccessor,
        IPeriodAuditLogRepository auditLogRepository,
        IUnitOfWork unitOfWork,
        ILogger<ReopenPeriodByGroupCommandHandler> logger)
        : base(unitOfWork, logger)
    {
        _workRepository = workRepository;
        _breakRepository = breakRepository;
        _lockLevelService = lockLevelService;
        _httpContextAccessor = httpContextAccessor;
        _auditLogRepository = auditLogRepository;
    }

    public async Task<int> Handle(ReopenPeriodByGroupCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithTransactionAsync(async () =>
        {
            if (string.IsNullOrWhiteSpace(request.Reason))
                throw new Domain.Exceptions.InvalidRequestException("A reason must be provided when reopening a period.");

            if (request.StartDate > request.EndDate)
                throw new Domain.Exceptions.InvalidRequestException("Start date must be before or equal to end date.");

            var isAdmin = _httpContextAccessor.HttpContext?.User?.IsInRole(Roles.Admin) == true;
            var isAuthorised = _httpContextAccessor.HttpContext?.User?.HasClaim(ClaimNames.IsAuthorised, "true") == true;

            if (!_lockLevelService.CanUnseal(WorkLockLevel.Closed, isAdmin, isAuthorised))
                throw new Domain.Exceptions.InvalidRequestException("You do not have permission to reopen periods.");

            var userName = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Unknown";

            int workCount;
            int breakCount;

            if (request.GroupId.HasValue)
            {
                workCount = await _workRepository.UnsealByPeriodAndGroup(request.StartDate, request.EndDate, request.GroupId.Value, WorkLockLevel.Closed, cancellationToken);
                breakCount = await _breakRepository.UnsealByPeriodAndGroup(request.StartDate, request.EndDate, request.GroupId.Value, WorkLockLevel.Closed, cancellationToken);
            }
            else
            {
                workCount = await _workRepository.UnsealByPeriod(request.StartDate, request.EndDate, WorkLockLevel.Closed, cancellationToken);
                breakCount = await _breakRepository.UnsealByPeriod(request.StartDate, request.EndDate, WorkLockLevel.Closed, cancellationToken);
            }

            var total = workCount + breakCount;

            await _auditLogRepository.AddAsync(new PeriodAuditLog
            {
                Action = PeriodAuditAction.Unseal,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                GroupId = request.GroupId,
                Reason = request.Reason.Trim(),
                AffectedCount = total,
                PerformedAt = DateTime.UtcNow,
                PerformedBy = userName
            }, cancellationToken);

            return total;
        },
        "reopening period (group-aware)",
        new { request.StartDate, request.EndDate, request.GroupId });
    }
}
