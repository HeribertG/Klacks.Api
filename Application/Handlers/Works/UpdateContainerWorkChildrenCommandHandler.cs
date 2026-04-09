// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for updating all children (sub-works, sub-breaks, and work changes) of a container work using the Savebar pattern.
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
using Klacks.Api.Domain.Services.Schedules;
using Klacks.Api.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Application.Handlers.Works;

public class UpdateContainerWorkChildrenCommandHandler : BaseHandler, IRequestHandler<UpdateContainerWorkChildrenCommand, ContainerWorkChildrenResource>
{
    private const string LockResourceType = "ContainerWork";

    private readonly DataBaseContext _context;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly EntityCollectionUpdateService _collectionUpdateService;
    private readonly IWorkNotificationFacade _notificationFacade;
    private readonly IContainerLockRepository _lockRepository;
    private readonly IUserService _userService;

    public UpdateContainerWorkChildrenCommandHandler(
        DataBaseContext context,
        ScheduleMapper scheduleMapper,
        IUnitOfWork unitOfWork,
        EntityCollectionUpdateService collectionUpdateService,
        IWorkNotificationFacade notificationFacade,
        IContainerLockRepository lockRepository,
        IUserService userService,
        ILogger<UpdateContainerWorkChildrenCommandHandler> logger)
        : base(logger)
    {
        _context = context;
        _scheduleMapper = scheduleMapper;
        _unitOfWork = unitOfWork;
        _collectionUpdateService = collectionUpdateService;
        _notificationFacade = notificationFacade;
        _lockRepository = lockRepository;
        _userService = userService;
    }

    public async Task<ContainerWorkChildrenResource> Handle(UpdateContainerWorkChildrenCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            if (request.Resource == null)
            {
                throw new ArgumentException("UpdateContainerWorkChildren request body is missing or could not be deserialized.");
            }

            var userId = _userService.GetId() ?? Guid.Empty;
            var instanceId = _userService.GetInstanceId() ?? string.Empty;
            var holdsLock = await _lockRepository.IsHeldBy(LockResourceType, request.WorkId, userId, instanceId, cancellationToken);
            if (!holdsLock)
            {
                throw new ContainerLockedException("Cannot save: container work is not locked by this session.");
            }

            var parentWork = await _context.Work
                .FirstOrDefaultAsync(w => w.Id == request.WorkId && !w.IsDeleted, cancellationToken);

            if (parentWork != null)
            {
                parentWork.StartBase = request.Resource.ParentStartBase;
                parentWork.EndBase = request.Resource.ParentEndBase;
            }

            var existingWorks = await _context.Work
                .Where(w => w.ParentWorkId == request.WorkId && !w.IsDeleted)
                .ToListAsync(cancellationToken);

            var existingBreaks = await _context.Break
                .Where(b => b.ParentWorkId == request.WorkId && !b.IsDeleted)
                .ToListAsync(cancellationToken);

            var existingSubWorkIds = existingWorks.Select(w => w.Id).ToList();

            var existingWorkChanges = existingSubWorkIds.Count > 0
                ? await _context.WorkChange
                    .Where(wc => existingSubWorkIds.Contains(wc.WorkId))
                    .ToListAsync(cancellationToken)
                : new List<WorkChange>();

            var updatedWorks = request.Resource.SubWorks.Select(_scheduleMapper.ToWorkEntity).ToList();
            var updatedBreaks = request.Resource.SubBreaks.Select(_scheduleMapper.ToBreakEntity).ToList();

            if (parentWork != null)
            {
                foreach (var b in updatedBreaks)
                {
                    b.ClientId = parentWork.ClientId;
                    if (b.CurrentDate == default)
                    {
                        b.CurrentDate = parentWork.CurrentDate;
                    }
                }
            }

            _collectionUpdateService.UpdateCollection(
                existingWorks,
                updatedWorks,
                request.WorkId,
                (work, parentId) => work.ParentWorkId = parentId);

            _collectionUpdateService.UpdateCollection(
                existingBreaks,
                updatedBreaks,
                request.WorkId,
                (b, parentId) => b.ParentWorkId = parentId);

            if (parentWork != null)
            {
                var breakAbsenceIds = updatedBreaks
                    .Select(b => b.AbsenceId)
                    .Where(id => id != Guid.Empty)
                    .Distinct()
                    .ToList();

                var unpaidAbsenceIds = breakAbsenceIds.Count > 0
                    ? await _context.Absence
                        .Where(a => breakAbsenceIds.Contains(a.Id) && a.IsUnpaid && !a.IsDeleted)
                        .Select(a => a.Id)
                        .ToListAsync(cancellationToken)
                    : new List<Guid>();

                var unpaidSet = unpaidAbsenceIds.ToHashSet();

                var unpaidBreakSpans = updatedBreaks
                    .Where(b => unpaidSet.Contains(b.AbsenceId))
                    .Select(b => (b.StartTime, b.EndTime))
                    .ToList();

                parentWork.WorkTime = ContainerWorkTimeCalculator.CalculatePaidHours(
                    parentWork.StartTime,
                    parentWork.EndTime,
                    unpaidBreakSpans);
            }

            var updatedWorkChanges = request.Resource.SubWorkChanges
                .Select(_scheduleMapper.ToWorkChangeEntity)
                .ToList();

            foreach (var existing in existingWorkChanges)
            {
                var updated = updatedWorkChanges.FirstOrDefault(wc => wc.Id == existing.Id);
                if (updated == null)
                {
                    _context.Entry(existing).State = EntityState.Deleted;
                }
                else
                {
                    var entry = _context.Entry(existing);
                    entry.CurrentValues.SetValues(updated);
                    entry.State = EntityState.Modified;
                }
            }

            foreach (var newWc in updatedWorkChanges.Where(wc => wc.Id == Guid.Empty || !existingWorkChanges.Any(e => e.Id == wc.Id)))
            {
                _context.Entry(newWc).State = EntityState.Added;
            }

            await _unitOfWork.CompleteAsync();

            if (parentWork != null)
            {
                var affectedShifts = new HashSet<(Guid ShiftId, DateOnly Date)>
                {
                    (parentWork.ShiftId, parentWork.CurrentDate)
                };

                var connectionId = _notificationFacade.GetConnectionId();
                await _notificationFacade.NotifyShiftStatsAsync(affectedShifts, connectionId, cancellationToken);
            }

            var reloadedWorks = await _context.Work
                .Include(w => w.Shift)
                    .ThenInclude(s => s!.Client)
                        .ThenInclude(c => c!.Addresses)
                .Where(w => w.ParentWorkId == request.WorkId)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            var reloadedBreaks = await _context.Break
                .Include(b => b.Absence)
                .Where(b => b.ParentWorkId == request.WorkId)
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
        }, nameof(Handle), new { request.WorkId });
    }
}
