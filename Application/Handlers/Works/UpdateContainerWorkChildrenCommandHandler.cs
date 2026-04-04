// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for updating all children (sub-works and sub-breaks) of a container work using the Savebar pattern.
/// </summary>
using Klacks.Api.Application.Commands.Works;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Infrastructure.Persistence;
using Klacks.Api.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Application.Handlers.Works;

public class UpdateContainerWorkChildrenCommandHandler : BaseHandler, IRequestHandler<UpdateContainerWorkChildrenCommand, ContainerWorkChildrenResource>
{
    private readonly DataBaseContext _context;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly EntityCollectionUpdateService _collectionUpdateService;
    private readonly IWorkNotificationFacade _notificationFacade;

    public UpdateContainerWorkChildrenCommandHandler(
        DataBaseContext context,
        ScheduleMapper scheduleMapper,
        IUnitOfWork unitOfWork,
        EntityCollectionUpdateService collectionUpdateService,
        IWorkNotificationFacade notificationFacade,
        ILogger<UpdateContainerWorkChildrenCommandHandler> logger)
        : base(logger)
    {
        _context = context;
        _scheduleMapper = scheduleMapper;
        _unitOfWork = unitOfWork;
        _collectionUpdateService = collectionUpdateService;
        _notificationFacade = notificationFacade;
    }

    public async Task<ContainerWorkChildrenResource> Handle(UpdateContainerWorkChildrenCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var parentWork = await _context.Work
                .AsNoTracking()
                .FirstOrDefaultAsync(w => w.Id == request.WorkId && !w.IsDeleted, cancellationToken);

            var existingWorks = await _context.Work
                .Where(w => w.ParentWorkId == request.WorkId && !w.IsDeleted)
                .ToListAsync(cancellationToken);

            var existingBreaks = await _context.Break
                .Where(b => b.ParentWorkId == request.WorkId && !b.IsDeleted)
                .ToListAsync(cancellationToken);

            var updatedWorks = request.Resource.SubWorks.Select(_scheduleMapper.ToWorkEntity).ToList();
            var updatedBreaks = request.Resource.SubBreaks.Select(_scheduleMapper.ToBreakEntity).ToList();

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
                .Where(w => w.ParentWorkId == request.WorkId && !w.IsDeleted)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            var reloadedBreaks = await _context.Break
                .Include(b => b.Absence)
                .Where(b => b.ParentWorkId == request.WorkId && !b.IsDeleted)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            return new ContainerWorkChildrenResource
            {
                SubWorks = reloadedWorks.Select(_scheduleMapper.ToWorkResource).ToList(),
                SubBreaks = reloadedBreaks.Select(_scheduleMapper.ToBreakResource).ToList()
            };
        }, nameof(Handle), new { request.WorkId });
    }
}
