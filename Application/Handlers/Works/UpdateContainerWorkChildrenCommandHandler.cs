// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for updating all children (sub-works, sub-breaks, and work changes) of a container work using the Savebar pattern.
/// Delegates business logic to IContainerWorkChildrenManager; handles lock verification, mapping, persistence, and notifications.
/// </summary>
using Klacks.Api.Application.Commands.Works;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Application.Handlers.Works;

public class UpdateContainerWorkChildrenCommandHandler : BaseHandler, IRequestHandler<UpdateContainerWorkChildrenCommand, ContainerWorkChildrenResource>
{
    private const string LockResourceType = "ContainerWork";

    private readonly DataBaseContext _context;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWorkNotificationFacade _notificationFacade;
    private readonly IContainerLockRepository _lockRepository;
    private readonly IUserService _userService;
    private readonly IContainerWorkChildrenManager _childrenManager;

    public UpdateContainerWorkChildrenCommandHandler(
        DataBaseContext context,
        ScheduleMapper scheduleMapper,
        IUnitOfWork unitOfWork,
        IWorkNotificationFacade notificationFacade,
        IContainerLockRepository lockRepository,
        IUserService userService,
        IContainerWorkChildrenManager childrenManager,
        ILogger<UpdateContainerWorkChildrenCommandHandler> logger)
        : base(logger)
    {
        _context = context;
        _scheduleMapper = scheduleMapper;
        _unitOfWork = unitOfWork;
        _notificationFacade = notificationFacade;
        _lockRepository = lockRepository;
        _userService = userService;
        _childrenManager = childrenManager;
    }

    public async Task<ContainerWorkChildrenResource> Handle(UpdateContainerWorkChildrenCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            if (request.Resource == null)
            {
                throw new ArgumentException("UpdateContainerWorkChildren request body is missing or could not be deserialized.");
            }

            await VerifyLockAsync(request.WorkId, cancellationToken);

            var updatedWorks = request.Resource.SubWorks.Select(_scheduleMapper.ToWorkEntity).ToList();
            var updatedBreaks = request.Resource.SubBreaks.Select(_scheduleMapper.ToBreakEntity).ToList();
            var updatedWorkChanges = request.Resource.SubWorkChanges.Select(_scheduleMapper.ToWorkChangeEntity).ToList();

            var parentWork = await _childrenManager.UpdateChildrenAsync(
                request.WorkId,
                request.Resource.ParentStartBase,
                request.Resource.ParentEndBase,
                request.Resource.ParentStartTime,
                request.Resource.ParentEndTime,
                updatedWorks,
                updatedBreaks,
                updatedWorkChanges,
                cancellationToken);

            await _unitOfWork.CompleteAsync();

            await NotifyAffectedShiftsAsync(parentWork, cancellationToken);

            return await BuildResponseAsync(request.WorkId, parentWork, cancellationToken);
        }, nameof(Handle), new { request.WorkId });
    }

    private async Task VerifyLockAsync(Guid workId, CancellationToken cancellationToken)
    {
        var userId = _userService.GetId() ?? Guid.Empty;
        var instanceId = _userService.GetInstanceId() ?? string.Empty;
        var holdsLock = await _lockRepository.IsHeldBy(LockResourceType, workId, userId, instanceId, cancellationToken);
        if (!holdsLock)
        {
            throw new ContainerLockedException("Cannot save: container work is not locked by this session.");
        }
    }

    private async Task NotifyAffectedShiftsAsync(Work? parentWork, CancellationToken cancellationToken)
    {
        if (parentWork == null) return;

        var affectedShifts = new HashSet<(Guid ShiftId, DateOnly Date)>
        {
            (parentWork.ShiftId, parentWork.CurrentDate)
        };

        var connectionId = _notificationFacade.GetConnectionId();
        await _notificationFacade.NotifyShiftStatsAsync(affectedShifts, connectionId, cancellationToken);
    }

    private async Task<ContainerWorkChildrenResource> BuildResponseAsync(
        Guid workId, Work? parentWork, CancellationToken cancellationToken)
    {
        var reloadedWorks = await _context.Work
            .Include(w => w.Shift)
                .ThenInclude(s => s!.Client)
                    .ThenInclude(c => c!.Addresses)
            .Where(w => w.ParentWorkId == workId)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var reloadedBreaks = await _context.Break
            .Include(b => b.Absence)
            .Where(b => b.ParentWorkId == workId)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var reloadedSubWorkIds = reloadedWorks.Select(w => w.Id).ToList();

        var reloadedWorkChanges = reloadedSubWorkIds.Count > 0
            ? await _context.WorkChange
                .Where(wc => reloadedSubWorkIds.Contains(wc.WorkId))
                .AsNoTracking()
                .ToListAsync(cancellationToken)
            : new List<WorkChange>();

        return new ContainerWorkChildrenResource
        {
            SubWorks = reloadedWorks.Select(_scheduleMapper.ToWorkResource).ToList(),
            SubBreaks = reloadedBreaks.Select(_scheduleMapper.ToBreakResource).ToList(),
            SubWorkChanges = reloadedWorkChanges.Select(_scheduleMapper.ToWorkChangeResource).ToList(),
            ParentStartBase = parentWork?.StartBase,
            ParentEndBase = parentWork?.EndBase,
            ParentTransportMode = parentWork?.TransportMode.HasValue == true ? (int)parentWork.TransportMode.Value : null,
            ParentWorkTime = parentWork?.WorkTime ?? 0m
        };
    }
}
