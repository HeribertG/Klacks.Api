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

            var startBase = parentWork?.StartBase;
            var endBase = parentWork?.EndBase;
            int? transportMode = parentWork?.TransportMode.HasValue == true ? (int)parentWork.TransportMode.Value : null;

            var needsFallback = parentWork != null
                && parentWork.ShiftId != Guid.Empty
                && (string.IsNullOrEmpty(startBase) || string.IsNullOrEmpty(endBase) || transportMode == null);

            if (needsFallback)
            {
                var weekday = (int)parentWork!.CurrentDate.DayOfWeek;
                var template = await _context.ContainerTemplate
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        t => t.ContainerId == parentWork.ShiftId
                            && t.Weekday == weekday
                            && t.IsHoliday == request.IsHoliday,
                        cancellationToken);

                if (template != null)
                {
                    if (string.IsNullOrEmpty(startBase)) startBase = template.StartBase;
                    if (string.IsNullOrEmpty(endBase)) endBase = template.EndBase;
                    if (transportMode == null) transportMode = (int)template.TransportMode;
                }
            }

            return new ContainerWorkChildrenResource
            {
                SubWorks = subWorks.Select(_scheduleMapper.ToWorkResource).ToList(),
                SubBreaks = subBreaks.Select(_scheduleMapper.ToBreakResource).ToList(),
                SubWorkChanges = subWorkChanges.Select(_scheduleMapper.ToWorkChangeResource).ToList(),
                ParentStartBase = startBase,
                ParentEndBase = endBase,
                ParentTransportMode = transportMode
            };
        }, nameof(Handle), new { request.WorkId, request.IsHoliday });
    }
}
