// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Commands.Works;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.DTOs.Schedules;
using System.Security.Claims;

namespace Klacks.Api.Application.Handlers.Works;

public class ConfirmWorkCommandHandler : BaseHandler, IRequestHandler<ConfirmWorkCommand, WorkResource?>
{
    private readonly IWorkRepository _workRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWorkLockLevelService _lockLevelService;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IContainerWorkCascadeService _cascadeService;

    public ConfirmWorkCommandHandler(
        IWorkRepository workRepository,
        IUnitOfWork unitOfWork,
        IWorkLockLevelService lockLevelService,
        ScheduleMapper scheduleMapper,
        IHttpContextAccessor httpContextAccessor,
        IContainerWorkCascadeService cascadeService,
        ILogger<ConfirmWorkCommandHandler> logger)
        : base(logger)
    {
        _workRepository = workRepository;
        _unitOfWork = unitOfWork;
        _lockLevelService = lockLevelService;
        _scheduleMapper = scheduleMapper;
        _httpContextAccessor = httpContextAccessor;
        _cascadeService = cascadeService;
    }

    public async Task<WorkResource?> Handle(ConfirmWorkCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var work = await _workRepository.Get(request.WorkId);
            if (work == null)
                throw new KeyNotFoundException($"Work with ID {request.WorkId} not found.");

            var isAdmin = _httpContextAccessor.HttpContext?.User?.IsInRole(Roles.Admin) == true;
            var isAuthorised = _httpContextAccessor.HttpContext?.User?.HasClaim(ClaimNames.IsAuthorised, "true") == true;

            var userName = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Unknown";

            _lockLevelService.Seal(work, WorkLockLevel.Confirmed, userName, isAdmin, isAuthorised);

            await _workRepository.Put(work);
            await _cascadeService.UpdateLockLevelAsync(work.Id, work.LockLevel, userName);
            await _unitOfWork.CompleteAsync();

            return _scheduleMapper.ToWorkResource(work);
        },
        "confirming work",
        new { WorkId = request.WorkId });
    }
}
