// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Lifts the lock of one work entry. Two refusals live here and they are not the same answer: an entry
/// that carries no lock at all cannot be unsealed by anybody, which is a state conflict (400); an entry
/// whose lock sits above the caller's role is a rights problem (403). Reporting the second as 400 told
/// the planner their request was malformed when in truth they simply may not undo a supervisor's
/// approval.
/// </summary>
/// <param name="request">Carries the id of the work entry to unseal</param>

using Klacks.Api.Application.Commands.Works;
using Klacks.Api.Application.Exceptions;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.Api.Application.Handlers.Works;

public class UnconfirmWorkCommandHandler : BaseHandler, IRequestHandler<UnconfirmWorkCommand, WorkResource?>
{
    private const string NotSealedMessage = "This entry is not sealed, so there is nothing to unseal.";

    private const string InsufficientRoleMessage =
        "Unsealing this entry needs a higher role than yours.";

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

            var isAdmin = _httpContextAccessor.HttpContext?.User?.IsInRole(Roles.Admin) == true;
            var isAuthorised = _httpContextAccessor.HttpContext?.User?.IsInRole(Roles.Authorised) == true;

            if (work.LockLevel == WorkLockLevel.None)
            {
                throw new InvalidRequestException(NotSealedMessage);
            }

            if (!_lockLevelService.CanUnseal(work.LockLevel, isAdmin, isAuthorised))
            {
                throw new ForbiddenException(InsufficientRoleMessage);
            }

            _lockLevelService.Unseal(work, isAdmin, isAuthorised);

            await _workRepository.Put(work);
            await _unitOfWork.CompleteAsync();

            return _scheduleMapper.ToWorkResource(work);
        },
        "unconfirming work",
        new { WorkId = request.WorkId });
    }
}
