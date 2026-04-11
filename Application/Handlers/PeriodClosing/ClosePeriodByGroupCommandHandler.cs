// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for sealing a period by group, or the entire period when no group is specified.
/// </summary>
/// <param name="request">Contains StartDate, EndDate, optional GroupId and optional Reason</param>

using Klacks.Api.Application.Commands.PeriodClosing;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using System.Security.Claims;

namespace Klacks.Api.Application.Handlers.PeriodClosing;

public class ClosePeriodByGroupCommandHandler : BaseHandler, IRequestHandler<ClosePeriodByGroupCommand, int>
{
    private readonly IWorkRepository _workRepository;
    private readonly IBreakRepository _breakRepository;
    private readonly IWorkLockLevelService _lockLevelService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IPeriodAuditLogRepository _auditLogRepository;

    public ClosePeriodByGroupCommandHandler(
        IWorkRepository workRepository,
        IBreakRepository breakRepository,
        IWorkLockLevelService lockLevelService,
        IHttpContextAccessor httpContextAccessor,
        IPeriodAuditLogRepository auditLogRepository,
        ILogger<ClosePeriodByGroupCommandHandler> logger)
        : base(logger)
    {
        _workRepository = workRepository;
        _breakRepository = breakRepository;
        _lockLevelService = lockLevelService;
        _httpContextAccessor = httpContextAccessor;
        _auditLogRepository = auditLogRepository;
    }

    public async Task<int> Handle(ClosePeriodByGroupCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            if (request.StartDate > request.EndDate)
                throw new Domain.Exceptions.InvalidRequestException("Start date must be before or equal to end date.");

            var isAdmin = _httpContextAccessor.HttpContext?.User?.IsInRole(Roles.Admin) == true;
            var isAuthorised = _httpContextAccessor.HttpContext?.User?.HasClaim(ClaimNames.IsAuthorised, "true") == true;

            if (!_lockLevelService.CanSeal(WorkLockLevel.None, WorkLockLevel.Closed, isAdmin, isAuthorised))
                throw new Domain.Exceptions.InvalidRequestException("You do not have permission to close periods.");

            var userName = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Unknown";

            int workCount;
            int breakCount;

            if (request.GroupId.HasValue)
            {
                workCount = await _workRepository.SealByPeriodAndGroup(request.StartDate, request.EndDate, request.GroupId.Value, WorkLockLevel.Closed, userName, cancellationToken);
                breakCount = await _breakRepository.SealByPeriodAndGroup(request.StartDate, request.EndDate, request.GroupId.Value, WorkLockLevel.Closed, userName, cancellationToken);
            }
            else
            {
                workCount = await _workRepository.SealByPeriod(request.StartDate, request.EndDate, WorkLockLevel.Closed, userName, cancellationToken);
                breakCount = await _breakRepository.SealByPeriod(request.StartDate, request.EndDate, WorkLockLevel.Closed, userName, cancellationToken);
            }

            var total = workCount + breakCount;

            await _auditLogRepository.AddAsync(new PeriodAuditLog
            {
                Action = PeriodAuditAction.Seal,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                GroupId = request.GroupId,
                Reason = request.Reason,
                AffectedCount = total,
                PerformedAt = DateTime.UtcNow,
                PerformedBy = userName
            }, cancellationToken);

            return total;
        },
        "closing period (group-aware)",
        new { request.StartDate, request.EndDate, request.GroupId });
    }
}
