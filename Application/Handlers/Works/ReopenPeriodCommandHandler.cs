using Klacks.Api.Application.Commands.Works;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Works;

public class ReopenPeriodCommandHandler : BaseHandler, IRequestHandler<ReopenPeriodCommand, int>
{
    private readonly IWorkRepository _workRepository;
    private readonly IBreakRepository _breakRepository;
    private readonly IWorkLockLevelService _lockLevelService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ReopenPeriodCommandHandler(
        IWorkRepository workRepository,
        IBreakRepository breakRepository,
        IWorkLockLevelService lockLevelService,
        IHttpContextAccessor httpContextAccessor,
        ILogger<ReopenPeriodCommandHandler> logger)
        : base(logger)
    {
        _workRepository = workRepository;
        _breakRepository = breakRepository;
        _lockLevelService = lockLevelService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<int> Handle(ReopenPeriodCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var isAdmin = _httpContextAccessor.HttpContext?.User?.IsInRole("Admin") == true;
            var isAuthorised = _httpContextAccessor.HttpContext?.User?.HasClaim("IsAuthorised", "true") == true;

            if (!_lockLevelService.CanUnseal(WorkLockLevel.Closed, isAdmin, isAuthorised))
                throw new Domain.Exceptions.InvalidRequestException("You do not have permission to reopen periods.");

            var workCount = await _workRepository.UnsealByPeriod(request.StartDate, request.EndDate, WorkLockLevel.Closed, cancellationToken);
            var breakCount = await _breakRepository.UnsealByPeriod(request.StartDate, request.EndDate, WorkLockLevel.Closed, cancellationToken);

            return workCount + breakCount;
        },
        "reopening period",
        new { request.StartDate, request.EndDate });
    }
}
