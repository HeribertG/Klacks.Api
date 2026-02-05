using Klacks.Api.Application.Commands.Works;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Presentation.DTOs.Schedules;
using System.Security.Claims;

namespace Klacks.Api.Application.Handlers.Works;

public class ConfirmWorkCommandHandler : BaseHandler, IRequestHandler<ConfirmWorkCommand, WorkResource?>
{
    private readonly IWorkRepository _workRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWorkLockLevelService _lockLevelService;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ConfirmWorkCommandHandler(
        IWorkRepository workRepository,
        IUnitOfWork unitOfWork,
        IWorkLockLevelService lockLevelService,
        ScheduleMapper scheduleMapper,
        IHttpContextAccessor httpContextAccessor,
        ILogger<ConfirmWorkCommandHandler> logger)
        : base(logger)
    {
        _workRepository = workRepository;
        _unitOfWork = unitOfWork;
        _lockLevelService = lockLevelService;
        _scheduleMapper = scheduleMapper;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<WorkResource?> Handle(ConfirmWorkCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var work = await _workRepository.Get(request.WorkId);
            if (work == null)
                throw new KeyNotFoundException($"Work with ID {request.WorkId} not found.");

            var isAdmin = _httpContextAccessor.HttpContext?.User?.IsInRole("Admin") == true;
            var isAuthorised = _httpContextAccessor.HttpContext?.User?.HasClaim("IsAuthorised", "true") == true;

            if (!_lockLevelService.CanSeal(work.LockLevel, WorkLockLevel.Confirmed, isAdmin, isAuthorised))
                throw new Domain.Exceptions.InvalidRequestException("Work entry cannot be confirmed in its current state.");

            var userName = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Unknown";

            work.LockLevel = WorkLockLevel.Confirmed;
            work.SealedAt = DateTime.UtcNow;
            work.SealedBy = userName;

            await _workRepository.Put(work);
            await _unitOfWork.CompleteAsync();

            return _scheduleMapper.ToWorkResource(work);
        },
        "confirming work",
        new { WorkId = request.WorkId });
    }
}
