using Klacks.Api.Application.Commands.Works;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Works;

public class RevokeDayApprovalCommandHandler : BaseHandler, IRequestHandler<RevokeDayApprovalCommand, int>
{
    private readonly IWorkRepository _workRepository;
    private readonly IBreakRepository _breakRepository;
    private readonly IWorkLockLevelService _lockLevelService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public RevokeDayApprovalCommandHandler(
        IWorkRepository workRepository,
        IBreakRepository breakRepository,
        IWorkLockLevelService lockLevelService,
        IHttpContextAccessor httpContextAccessor,
        ILogger<RevokeDayApprovalCommandHandler> logger)
        : base(logger)
    {
        _workRepository = workRepository;
        _breakRepository = breakRepository;
        _lockLevelService = lockLevelService;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<int> Handle(RevokeDayApprovalCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var isAdmin = _httpContextAccessor.HttpContext?.User?.IsInRole("Admin") == true;
            var isAuthorised = _httpContextAccessor.HttpContext?.User?.HasClaim("IsAuthorised", "true") == true;

            if (!_lockLevelService.CanUnseal(WorkLockLevel.Approved, isAdmin, isAuthorised))
                throw new Domain.Exceptions.InvalidRequestException("You do not have permission to revoke day approvals.");

            var workCount = await _workRepository.UnsealByDayAndGroup(request.Date, request.GroupId, WorkLockLevel.Approved, cancellationToken);
            var breakCount = await _breakRepository.UnsealByDayAndGroup(request.Date, request.GroupId, WorkLockLevel.Approved, cancellationToken);

            return workCount + breakCount;
        },
        "revoking day approval",
        new { request.Date, request.GroupId });
    }
}
