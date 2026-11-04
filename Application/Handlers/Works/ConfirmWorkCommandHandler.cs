// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Confirms a single work entry and records the confirmation in the period audit log, so the level
/// on which an identity is actually read is no longer the only unprotocolled one. A Work owned by a client
/// outside the caller's group visibility is refused exactly like a Work that does not exist; nothing is sealed.
/// Confirming states that the service took place, so a service is only confirmed from its end on (company clock, a night
/// shift ends on the following day); this holds for a scenario Work as well. A service inside a container is never confirmed
/// on its own: the container confirms it with its children (IContainerWorkCascadeService).
/// </summary>
/// <param name="rowAccessGuard">Decides whether the calling user may see the owning client and the stored row's scenario</param>
/// <param name="companyClock">The company-local now the end of the service is measured against</param>

using Klacks.Api.Application.Commands.Works;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Services.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.DTOs.Schedules;
using System.Globalization;
using System.Security.Claims;

namespace Klacks.Api.Application.Handlers.Works;

public class ConfirmWorkCommandHandler : BaseHandler, IRequestHandler<ConfirmWorkCommand, WorkResource?>
{
    private const int SingleEntryAffectedCount = 1;

    private readonly IWorkRepository _workRepository;
    private readonly IScheduleRowAccessGuard _rowAccessGuard;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWorkLockLevelService _lockLevelService;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IContainerWorkCascadeService _cascadeService;
    private readonly IPeriodAuditLogRepository _auditLogRepository;
    private readonly IUserService _userService;
    private readonly ICompanyClock _companyClock;

    public ConfirmWorkCommandHandler(
        IWorkRepository workRepository,
        IScheduleRowAccessGuard rowAccessGuard,
        IUnitOfWork unitOfWork,
        IWorkLockLevelService lockLevelService,
        ScheduleMapper scheduleMapper,
        IHttpContextAccessor httpContextAccessor,
        IContainerWorkCascadeService cascadeService,
        IPeriodAuditLogRepository auditLogRepository,
        IUserService userService,
        ICompanyClock companyClock,
        ILogger<ConfirmWorkCommandHandler> logger)
        : base(logger)
    {
        _workRepository = workRepository;
        _rowAccessGuard = rowAccessGuard;
        _unitOfWork = unitOfWork;
        _lockLevelService = lockLevelService;
        _scheduleMapper = scheduleMapper;
        _httpContextAccessor = httpContextAccessor;
        _cascadeService = cascadeService;
        _auditLogRepository = auditLogRepository;
        _userService = userService;
        _companyClock = companyClock;
    }

    public async Task<WorkResource?> Handle(ConfirmWorkCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var work = await _workRepository.Get(request.WorkId);
            if (work == null || !await _rowAccessGuard.IsVisibleAsync(work.ClientId, work.AnalyseToken, cancellationToken))
                throw new KeyNotFoundException($"Work with ID {request.WorkId} not found.");

            if (work.ParentWorkId != null)
            {
                throw new ContainerChildConfirmRefusedException();
            }

            var companyNow = (await _companyClock.GetNowAsync(cancellationToken)).DateTime;
            if (!WorkServiceEnd.HasEnded(work, companyNow))
            {
                throw new InvalidRequestException(
                    $"The service has not ended yet (it ends {WorkServiceEnd.EndOf(work).ToString(DateTimeFormats.IsoDateHourMinute, CultureInfo.InvariantCulture)}) and can only be confirmed after its end.");
            }

            var isAdmin = _httpContextAccessor.HttpContext?.User?.IsInRole(Roles.Admin) == true;
            var isAuthorised = _httpContextAccessor.HttpContext?.User?.IsInRole(Roles.Authorised) == true;

            var userName = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? AuditActorDefaults.UnknownActor;

            _lockLevelService.Seal(work, WorkLockLevel.Confirmed, userName, isAdmin, isAuthorised);

            await _workRepository.Put(work);
            await _cascadeService.UpdateLockLevelAsync(work.Id, work.LockLevel, userName);

            await _auditLogRepository.AddAsync(
                PeriodAuditLog.For(
                    PeriodAuditAction.ConfirmWork,
                    work.CurrentDate,
                    work.CurrentDate,
                    null,
                    null,
                    SingleEntryAffectedCount,
                    userName,
                    _userService.GetDisplayName()),
                cancellationToken);

            await _unitOfWork.CompleteAsync();

            return _scheduleMapper.ToWorkResource(work);
        },
        "confirming work",
        new { WorkId = request.WorkId });
    }
}
