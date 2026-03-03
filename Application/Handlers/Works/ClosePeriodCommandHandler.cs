// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Commands.Works;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Infrastructure.Mediator;
using System.Security.Claims;

namespace Klacks.Api.Application.Handlers.Works;

public class ClosePeriodCommandHandler : BaseHandler, IRequestHandler<ClosePeriodCommand, int>
{
    private readonly IWorkRepository _workRepository;
    private readonly IBreakRepository _breakRepository;
    private readonly IWorkLockLevelService _lockLevelService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ClosePeriodCommandHandler(
        IWorkRepository workRepository,
        IBreakRepository breakRepository,
        IWorkLockLevelService lockLevelService,
        IHttpContextAccessor httpContextAccessor,
        ILogger<ClosePeriodCommandHandler> logger)
        : base(logger)
    {
        _workRepository = workRepository;
        _breakRepository = breakRepository;
        _lockLevelService = lockLevelService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<int> Handle(ClosePeriodCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var isAdmin = _httpContextAccessor.HttpContext?.User?.IsInRole("Admin") == true;
            var isAuthorised = _httpContextAccessor.HttpContext?.User?.HasClaim("IsAuthorised", "true") == true;

            if (!_lockLevelService.CanSeal(WorkLockLevel.None, WorkLockLevel.Closed, isAdmin, isAuthorised))
                throw new Domain.Exceptions.InvalidRequestException("You do not have permission to close periods.");

            var userName = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Unknown";

            var workCount = await _workRepository.SealByPeriod(request.StartDate, request.EndDate, WorkLockLevel.Closed, userName, cancellationToken);
            var breakCount = await _breakRepository.SealByPeriod(request.StartDate, request.EndDate, WorkLockLevel.Closed, userName, cancellationToken);

            return workCount + breakCount;
        },
        "closing period",
        new { request.StartDate, request.EndDate });
    }
}
