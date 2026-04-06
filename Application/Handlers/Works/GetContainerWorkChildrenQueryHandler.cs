// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for retrieving all children (sub-works, sub-breaks, and work changes) of a container work.
/// </summary>
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Queries.Works;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Application.Handlers.Works;

public class GetContainerWorkChildrenQueryHandler : BaseHandler, IRequestHandler<GetContainerWorkChildrenQuery, ContainerWorkChildrenResource>
{
    private readonly DataBaseContext _context;
    private readonly ScheduleMapper _scheduleMapper;

    public GetContainerWorkChildrenQueryHandler(
        DataBaseContext context,
        ScheduleMapper scheduleMapper,
        ILogger<GetContainerWorkChildrenQueryHandler> logger)
        : base(logger)
    {
        _context = context;
        _scheduleMapper = scheduleMapper;
    }

    public async Task<ContainerWorkChildrenResource> Handle(GetContainerWorkChildrenQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var parentWork = await _context.Work
                .AsNoTracking()
                .FirstOrDefaultAsync(w => w.Id == request.WorkId, cancellationToken);

            var subWorks = await _context.Work
                .Include(w => w.Shift)
                    .ThenInclude(s => s!.Client)
                        .ThenInclude(c => c!.Addresses)
                .Where(w => w.ParentWorkId == request.WorkId)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            var subBreaks = await _context.Break
                .Include(b => b.Absence)
                .Where(b => b.ParentWorkId == request.WorkId)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            var subWorkIds = subWorks.Select(w => w.Id).ToList();

            var subWorkChanges = subWorkIds.Count > 0
                ? await _context.WorkChange
                    .Where(wc => subWorkIds.Contains(wc.WorkId))
                    .AsNoTracking()
                    .ToListAsync(cancellationToken)
                : new List<WorkChange>();

            return new ContainerWorkChildrenResource
            {
                SubWorks = subWorks.Select(_scheduleMapper.ToWorkResource).ToList(),
                SubBreaks = subBreaks.Select(_scheduleMapper.ToBreakResource).ToList(),
                SubWorkChanges = subWorkChanges.Select(_scheduleMapper.ToWorkChangeResource).ToList(),
                ParentStartBase = parentWork?.StartBase,
                ParentEndBase = parentWork?.EndBase,
                ParentTransportMode = parentWork?.TransportMode.HasValue == true ? (int)parentWork.TransportMode.Value : null
            };
        }, nameof(Handle), new { request.WorkId });
    }
}
