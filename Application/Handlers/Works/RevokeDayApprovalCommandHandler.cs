// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Lifts the day approval of every work and break entry of one day within one group. A group outside the
/// caller's group visibility is answered like a group without entries: nothing is unsealed and the
/// affected count is zero. A day approved by another group (its SealedDay approval row) cannot be revoked here -
/// only the approving group lifts its approval; a day without any approval row (approved before rows existed) is
/// revoked as before. The group's own approval row is removed together with the entry approvals.
/// </summary>
/// <param name="sealedDayRepository">Reads and removes the day approval rows</param>
/// <param name="groupVisibilityGuard">Decides whether the calling user may write the group</param>

using Klacks.Api.Application.Commands.Works;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Schedules;
using System.Security.Claims;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Works;

public class RevokeDayApprovalCommandHandler : BaseHandler, IRequestHandler<RevokeDayApprovalCommand, int>
{
    private const int NoEntriesAffected = 0;

    private readonly IWorkRepository _workRepository;
    private readonly IGroupVisibilityGuard _groupVisibilityGuard;
    private readonly IBreakRepository _breakRepository;
    private readonly IWorkLockLevelService _lockLevelService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ISealedDayRepository _sealedDayRepository;

    public RevokeDayApprovalCommandHandler(
        IWorkRepository workRepository,
        IGroupVisibilityGuard groupVisibilityGuard,
        IBreakRepository breakRepository,
        IWorkLockLevelService lockLevelService,
        IHttpContextAccessor httpContextAccessor,
        ISealedDayRepository sealedDayRepository,
        ILogger<RevokeDayApprovalCommandHandler> logger)
        : base(logger)
    {
        _workRepository = workRepository;
        _groupVisibilityGuard = groupVisibilityGuard;
        _breakRepository = breakRepository;
        _lockLevelService = lockLevelService;
        _httpContextAccessor = httpContextAccessor;
        _sealedDayRepository = sealedDayRepository;
    }

    public async Task<int> Handle(RevokeDayApprovalCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var isAdmin = _httpContextAccessor.HttpContext?.User?.IsInRole(Roles.Admin) == true;
            var isAuthorised = _httpContextAccessor.HttpContext?.User?.IsInRole(Roles.Authorised) == true;

            if (!_lockLevelService.CanUnseal(WorkLockLevel.Approved, isAdmin, isAuthorised))
                throw new Domain.Exceptions.InvalidRequestException("You do not have permission to revoke day approvals.");

            if (!await _groupVisibilityGuard.IsGroupVisibleAsync(request.GroupId, cancellationToken))
            {
                return NoEntriesAffected;
            }

            var approvals = await _sealedDayRepository.GetDayApprovalsAsync(request.Date, cancellationToken);
            var ownApproval = approvals.Any(a => a.GroupId == request.GroupId);
            if (!ownApproval && approvals.Count > 0)
            {
                throw new Domain.Exceptions.InvalidRequestException(
                    "The day was approved by another group; only that group can revoke the approval.");
            }

            var workCount = await _workRepository.UnsealByDayAndGroup(request.Date, request.GroupId, WorkLockLevel.Approved, cancellationToken);
            var breakCount = await _breakRepository.UnsealByDayAndGroup(request.Date, request.GroupId, WorkLockLevel.Approved, cancellationToken);

            if (ownApproval)
            {
                var userName = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? AuditActorDefaults.UnknownActor;
                await _sealedDayRepository.SoftDeleteDayApprovalAsync(request.Date, request.GroupId, userName, cancellationToken);
            }

            return workCount + breakCount;
        },
        "revoking day approval",
        new { request.Date, request.GroupId });
    }
}
