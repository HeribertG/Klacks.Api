// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Removes the confirmation seal from a single break entry. A break owned by a client outside the caller's
/// group visibility is refused exactly like a break that does not exist; nothing is unsealed.
/// </summary>
/// <param name="rowAccessGuard">Decides whether the calling user may see the owning client and the stored row's scenario</param>

using Klacks.Api.Application.Commands.Breaks;
using Klacks.Api.Application.Exceptions;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.Api.Application.Handlers.Breaks;

public class UnconfirmBreakCommandHandler : BaseHandler, IRequestHandler<UnconfirmBreakCommand, BreakResource?>
{
    private const string NotSealedMessage = "This entry is not sealed, so there is nothing to unseal.";

    private const string InsufficientRoleMessage = "Unsealing this entry needs a higher role than yours.";

    private readonly IBreakRepository _breakRepository;
    private readonly IScheduleRowAccessGuard _rowAccessGuard;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWorkLockLevelService _lockLevelService;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IBreakUserContextProvider _userContextProvider;

    public UnconfirmBreakCommandHandler(
        IBreakRepository breakRepository,
        IScheduleRowAccessGuard rowAccessGuard,
        IUnitOfWork unitOfWork,
        IWorkLockLevelService lockLevelService,
        ScheduleMapper scheduleMapper,
        IBreakUserContextProvider userContextProvider,
        ILogger<UnconfirmBreakCommandHandler> logger)
        : base(logger)
    {
        _breakRepository = breakRepository;
        _rowAccessGuard = rowAccessGuard;
        _unitOfWork = unitOfWork;
        _lockLevelService = lockLevelService;
        _scheduleMapper = scheduleMapper;
        _userContextProvider = userContextProvider;
    }

    public async Task<BreakResource?> Handle(UnconfirmBreakCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var breakEntry = await _breakRepository.Get(request.BreakId);
            if (breakEntry == null || !await _rowAccessGuard.IsVisibleAsync(breakEntry.ClientId, breakEntry.AnalyseToken, cancellationToken))
                throw new KeyNotFoundException($"Break with ID {request.BreakId} not found.");

            var ctx = _userContextProvider.GetUserContext();
            if (breakEntry.LockLevel == WorkLockLevel.None)
            {
                throw new InvalidRequestException(NotSealedMessage);
            }

            if (!_lockLevelService.CanUnseal(breakEntry.LockLevel, ctx.IsAdmin, ctx.IsAuthorised))
            {
                throw new ForbiddenException(InsufficientRoleMessage);
            }

            _lockLevelService.Unseal(breakEntry, ctx.IsAdmin, ctx.IsAuthorised);

            await _breakRepository.Put(breakEntry);
            await _unitOfWork.CompleteAsync();

            return _scheduleMapper.ToBreakResource(breakEntry);
        },
        "unconfirming break",
        new { BreakId = request.BreakId });
    }
}
