// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Commands.Breaks;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.Api.Application.Handlers.Breaks;

public class ConfirmBreakCommandHandler : BaseHandler, IRequestHandler<ConfirmBreakCommand, BreakResource?>
{
    private readonly IBreakRepository _breakRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWorkLockLevelService _lockLevelService;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IBreakUserContextProvider _userContextProvider;

    public ConfirmBreakCommandHandler(
        IBreakRepository breakRepository,
        IUnitOfWork unitOfWork,
        IWorkLockLevelService lockLevelService,
        ScheduleMapper scheduleMapper,
        IBreakUserContextProvider userContextProvider,
        ILogger<ConfirmBreakCommandHandler> logger)
        : base(logger)
    {
        _breakRepository = breakRepository;
        _unitOfWork = unitOfWork;
        _lockLevelService = lockLevelService;
        _scheduleMapper = scheduleMapper;
        _userContextProvider = userContextProvider;
    }

    public async Task<BreakResource?> Handle(ConfirmBreakCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var breakEntry = await _breakRepository.Get(request.BreakId);
            if (breakEntry == null)
                throw new KeyNotFoundException($"Break with ID {request.BreakId} not found.");

            var ctx = _userContextProvider.GetUserContext();
            _lockLevelService.Seal(breakEntry, WorkLockLevel.Confirmed, ctx.UserName, ctx.IsAdmin, ctx.IsAuthorised);

            await _breakRepository.Put(breakEntry);
            await _unitOfWork.CompleteAsync();

            return _scheduleMapper.ToBreakResource(breakEntry);
        },
        "confirming break",
        new { BreakId = request.BreakId });
    }
}
