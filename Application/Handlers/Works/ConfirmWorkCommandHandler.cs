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

            var currentLevel = work.ConfirmedAt != null ? WorkLockLevel.Confirmed : WorkLockLevel.None;
            if (!_lockLevelService.CanConfirm(currentLevel))
                throw new Domain.Exceptions.InvalidRequestException("Work entry cannot be confirmed in its current state.");

            var userName = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Unknown";

            work.ConfirmedAt = DateTime.UtcNow;
            work.ConfirmedBy = userName;

            await _workRepository.Put(work);
            await _unitOfWork.CompleteAsync();

            return _scheduleMapper.ToWorkResource(work);
        },
        "confirming work",
        new { WorkId = request.WorkId });
    }
}
