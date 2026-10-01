// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Confirms a single break entry and records the confirmation in the period audit log, so the level
/// on which an identity is actually read is no longer the only unprotocolled one. A break owned by a client
/// outside the caller's group visibility is refused exactly like a break that does not exist; nothing is sealed.
/// </summary>
/// <param name="clientVisibilityGuard">Decides whether the calling user may write for the owning client</param>

using Klacks.Api.Application.Commands.Breaks;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.Api.Application.Handlers.Breaks;

public class ConfirmBreakCommandHandler : BaseHandler, IRequestHandler<ConfirmBreakCommand, BreakResource?>
{
    private const int SingleEntryAffectedCount = 1;

    private readonly IBreakRepository _breakRepository;
    private readonly IClientVisibilityGuard _clientVisibilityGuard;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWorkLockLevelService _lockLevelService;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IBreakUserContextProvider _userContextProvider;
    private readonly IPeriodAuditLogRepository _auditLogRepository;
    private readonly IUserService _userService;

    public ConfirmBreakCommandHandler(
        IBreakRepository breakRepository,
        IClientVisibilityGuard clientVisibilityGuard,
        IUnitOfWork unitOfWork,
        IWorkLockLevelService lockLevelService,
        ScheduleMapper scheduleMapper,
        IBreakUserContextProvider userContextProvider,
        IPeriodAuditLogRepository auditLogRepository,
        IUserService userService,
        ILogger<ConfirmBreakCommandHandler> logger)
        : base(logger)
    {
        _breakRepository = breakRepository;
        _clientVisibilityGuard = clientVisibilityGuard;
        _unitOfWork = unitOfWork;
        _lockLevelService = lockLevelService;
        _scheduleMapper = scheduleMapper;
        _userContextProvider = userContextProvider;
        _auditLogRepository = auditLogRepository;
        _userService = userService;
    }

    public async Task<BreakResource?> Handle(ConfirmBreakCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var breakEntry = await _breakRepository.Get(request.BreakId);
            if (breakEntry == null || !await _clientVisibilityGuard.IsVisibleAsync(breakEntry.ClientId, cancellationToken))
                throw new KeyNotFoundException($"Break with ID {request.BreakId} not found.");

            var ctx = _userContextProvider.GetUserContext();
            _lockLevelService.Seal(breakEntry, WorkLockLevel.Confirmed, ctx.UserName, ctx.IsAdmin, ctx.IsAuthorised);

            await _breakRepository.Put(breakEntry);

            await _auditLogRepository.AddAsync(
                PeriodAuditLog.For(
                    PeriodAuditAction.ConfirmBreak,
                    breakEntry.CurrentDate,
                    breakEntry.CurrentDate,
                    null,
                    null,
                    SingleEntryAffectedCount,
                    ctx.UserName,
                    _userService.GetDisplayName()),
                cancellationToken);

            await _unitOfWork.CompleteAsync();

            return _scheduleMapper.ToBreakResource(breakEntry);
        },
        "confirming break",
        new { BreakId = request.BreakId });
    }
}
