// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Lifts the day approval of every work and break entry of one day within one group. A group outside the
/// caller's group visibility is answered like a group without entries: nothing is unsealed and the
/// affected count is zero.
/// </summary>
/// <param name="groupVisibilityGuard">Decides whether the calling user may write the group</param>

using Klacks.Api.Application.Commands.Works;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
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

    public RevokeDayApprovalCommandHandler(
        IWorkRepository workRepository,
        IGroupVisibilityGuard groupVisibilityGuard,
        IBreakRepository breakRepository,
        IWorkLockLevelService lockLevelService,
        IHttpContextAccessor httpContextAccessor,
        ILogger<RevokeDayApprovalCommandHandler> logger)
        : base(logger)
    {
        _workRepository = workRepository;
        _groupVisibilityGuard = groupVisibilityGuard;
        _breakRepository = breakRepository;
        _lockLevelService = lockLevelService;
        _httpContextAccessor = httpContextAccessor;
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

            var workCount = await _workRepository.UnsealByDayAndGroup(request.Date, request.GroupId, WorkLockLevel.Approved, cancellationToken);
            var breakCount = await _breakRepository.UnsealByDayAndGroup(request.Date, request.GroupId, WorkLockLevel.Approved, cancellationToken);

            return workCount + breakCount;
        },
        "revoking day approval",
        new { request.Date, request.GroupId });
    }
}
