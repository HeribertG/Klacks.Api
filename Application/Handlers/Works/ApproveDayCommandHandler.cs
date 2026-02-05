using Klacks.Api.Application.Commands.Works;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Infrastructure.Mediator;
using System.Security.Claims;

namespace Klacks.Api.Application.Handlers.Works;

public class ApproveDayCommandHandler : BaseHandler, IRequestHandler<ApproveDayCommand, int>
{
    private readonly IWorkRepository _workRepository;
    private readonly IBreakRepository _breakRepository;
    private readonly IWorkLockLevelService _lockLevelService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ApproveDayCommandHandler(
        IWorkRepository workRepository,
        IBreakRepository breakRepository,
        IWorkLockLevelService lockLevelService,
        IHttpContextAccessor httpContextAccessor,
        ILogger<ApproveDayCommandHandler> logger)
        : base(logger)
    {
        _workRepository = workRepository;
        _breakRepository = breakRepository;
        _lockLevelService = lockLevelService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<int> Handle(ApproveDayCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var isAdmin = _httpContextAccessor.HttpContext?.User?.IsInRole("Admin") == true;
            var isAuthorised = _httpContextAccessor.HttpContext?.User?.HasClaim("IsAuthorised", "true") == true;

            if (!_lockLevelService.CanSeal(WorkLockLevel.None, WorkLockLevel.Approved, isAdmin, isAuthorised))
                throw new Domain.Exceptions.InvalidRequestException("You do not have permission to approve days.");

            var userName = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Unknown";

            var workCount = await _workRepository.SealByDayAndGroup(request.Date, request.GroupId, WorkLockLevel.Approved, userName, cancellationToken);
            var breakCount = await _breakRepository.SealByDayAndGroup(request.Date, request.GroupId, WorkLockLevel.Approved, userName, cancellationToken);

            return workCount + breakCount;
        },
        "approving day",
        new { request.Date, request.GroupId });
    }
}
