using Klacks.Api.Application.Commands.Works;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Presentation.DTOs.Schedules;

namespace Klacks.Api.Application.Handlers.Works;

public class UnconfirmWorkCommandHandler : BaseHandler, IRequestHandler<UnconfirmWorkCommand, WorkResource?>
{
    private readonly IWorkRepository _workRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWorkLockLevelService _lockLevelService;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UnconfirmWorkCommandHandler(
        IWorkRepository workRepository,
        IUnitOfWork unitOfWork,
        IWorkLockLevelService lockLevelService,
        ScheduleMapper scheduleMapper,
        IHttpContextAccessor httpContextAccessor,
        ILogger<UnconfirmWorkCommandHandler> logger)
        : base(logger)
    {
        _workRepository = workRepository;
        _unitOfWork = unitOfWork;
        _lockLevelService = lockLevelService;
        _scheduleMapper = scheduleMapper;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<WorkResource?> Handle(UnconfirmWorkCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var work = await _workRepository.Get(request.WorkId);
            if (work == null)
                throw new KeyNotFoundException($"Work with ID {request.WorkId} not found.");

            var isAdmin = _httpContextAccessor.HttpContext?.User?.IsInRole("Admin") == true;
            var isAuthorised = _httpContextAccessor.HttpContext?.User?.HasClaim("IsAuthorised", "true") == true;

            if (!_lockLevelService.CanUnseal(work.LockLevel, isAdmin, isAuthorised))
                throw new Domain.Exceptions.InvalidRequestException("Work entry cannot be unsealed in its current state.");

            work.LockLevel = WorkLockLevel.None;
            work.SealedAt = null;
            work.SealedBy = null;

            await _workRepository.Put(work);
            await _unitOfWork.CompleteAsync();

            return _scheduleMapper.ToWorkResource(work);
        },
        "unconfirming work",
        new { WorkId = request.WorkId });
    }
}
