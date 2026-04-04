// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for retrieving all children (sub-works and sub-breaks) of a container work.
/// </summary>
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Queries.Works;
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
            var subWorks = await _context.Work
                .Include(w => w.Shift)
                .Where(w => w.ParentWorkId == request.WorkId && !w.IsDeleted)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            var subBreaks = await _context.Break
                .Include(b => b.Absence)
                .Where(b => b.ParentWorkId == request.WorkId && !b.IsDeleted)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            return new ContainerWorkChildrenResource
            {
                SubWorks = subWorks.Select(_scheduleMapper.ToWorkResource).ToList(),
                SubBreaks = subBreaks.Select(_scheduleMapper.ToBreakResource).ToList()
            };
        }, nameof(Handle), new { request.WorkId });
    }
}
