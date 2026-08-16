// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Approves every work and break entry of one day within one group and writes the matching audit
/// record. Runs in a transaction so the two bulk updates and the audit row either all land or none
/// do - without it the audit entry would never be saved at all, because the bulk updates bypass the
/// change tracker while the audit row does not.
/// </summary>

using Klacks.Api.Application.Commands.Works;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using System.Security.Claims;

namespace Klacks.Api.Application.Handlers.Works;

public class ApproveDayCommandHandler : BaseTransactionHandler, IRequestHandler<ApproveDayCommand, int>
{
    private readonly IWorkRepository _workRepository;
    private readonly IBreakRepository _breakRepository;
    private readonly IWorkLockLevelService _lockLevelService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IPeriodAuditLogRepository _auditLogRepository;
    private readonly IUserService _userService;

    public ApproveDayCommandHandler(
        IWorkRepository workRepository,
        IBreakRepository breakRepository,
        IWorkLockLevelService lockLevelService,
        IHttpContextAccessor httpContextAccessor,
        IPeriodAuditLogRepository auditLogRepository,
        IUserService userService,
        IUnitOfWork unitOfWork,
        ILogger<ApproveDayCommandHandler> logger)
        : base(unitOfWork, logger)
    {
        _workRepository = workRepository;
        _breakRepository = breakRepository;
        _lockLevelService = lockLevelService;
        _httpContextAccessor = httpContextAccessor;
        _auditLogRepository = auditLogRepository;
        _userService = userService;
    }

    public async Task<int> Handle(ApproveDayCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithTransactionAsync(async () =>
        {
            var isAdmin = _httpContextAccessor.HttpContext?.User?.IsInRole(Roles.Admin) == true;
            var isAuthorised = _httpContextAccessor.HttpContext?.User?.IsInRole(Roles.Authorised) == true;

            if (!_lockLevelService.CanSeal(WorkLockLevel.None, WorkLockLevel.Approved, isAdmin, isAuthorised))
                throw new Domain.Exceptions.InvalidRequestException("You do not have permission to approve days.");

            var userName = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? AuditActorDefaults.UnknownActor;

            var workCount = await _workRepository.SealByDayAndGroup(request.Date, request.GroupId, WorkLockLevel.Approved, userName, cancellationToken);
            var breakCount = await _breakRepository.SealByDayAndGroup(request.Date, request.GroupId, WorkLockLevel.Approved, userName, cancellationToken);

            var affected = workCount + breakCount;

            await _auditLogRepository.AddAsync(
                PeriodAuditLog.For(
                    PeriodAuditAction.ApproveDay,
                    request.Date,
                    request.Date,
                    request.GroupId,
                    null,
                    affected,
                    userName,
                    _userService.GetDisplayName()),
                cancellationToken);

            return affected;
        },
        "approving day",
        new { request.Date, request.GroupId });
    }
}
