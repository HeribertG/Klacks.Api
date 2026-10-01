// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Removes the confirmation seal from a single break entry. A break owned by a client outside the caller's
/// group visibility is refused exactly like a break that does not exist; nothing is unsealed.
/// </summary>
/// <param name="clientVisibilityGuard">Decides whether the calling user may write for the owning client</param>

using Klacks.Api.Application.Commands.Breaks;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.Api.Application.Handlers.Breaks;

public class UnconfirmBreakCommandHandler : BaseHandler, IRequestHandler<UnconfirmBreakCommand, BreakResource?>
{
    private readonly IBreakRepository _breakRepository;
    private readonly IClientVisibilityGuard _clientVisibilityGuard;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWorkLockLevelService _lockLevelService;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IBreakUserContextProvider _userContextProvider;

    public UnconfirmBreakCommandHandler(
        IBreakRepository breakRepository,
        IClientVisibilityGuard clientVisibilityGuard,
        IUnitOfWork unitOfWork,
        IWorkLockLevelService lockLevelService,
        ScheduleMapper scheduleMapper,
        IBreakUserContextProvider userContextProvider,
        ILogger<UnconfirmBreakCommandHandler> logger)
        : base(logger)
    {
        _breakRepository = breakRepository;
        _clientVisibilityGuard = clientVisibilityGuard;
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
            if (breakEntry == null || !await _clientVisibilityGuard.IsVisibleAsync(breakEntry.ClientId, cancellationToken))
                throw new KeyNotFoundException($"Break with ID {request.BreakId} not found.");

            var ctx = _userContextProvider.GetUserContext();
            _lockLevelService.Unseal(breakEntry, ctx.IsAdmin, ctx.IsAuthorised);

            await _breakRepository.Put(breakEntry);
            await _unitOfWork.CompleteAsync();

            return _scheduleMapper.ToBreakResource(breakEntry);
        },
        "unconfirming break",
        new { BreakId = request.BreakId });
    }
}
