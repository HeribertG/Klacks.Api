// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Schedules;

/// <summary>
/// Repository for ScheduleCommand entity with a custom Get override and a scoped clients/date-range query.
/// </summary>
public class ScheduleCommandRepository : BaseRepository<ScheduleCommand>, IScheduleCommandRepository
{
    public ScheduleCommandRepository(DataBaseContext context, ILogger<ScheduleCommand> logger)
        : base(context, logger)
    {
    }

    public override async Task<ScheduleCommand?> Get(Guid id)
    {
        return await context.Set<ScheduleCommand>()
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<IReadOnlyList<ScheduleCommand>> GetByClientsAndDateRangeAsync(
        IReadOnlyList<Guid> clientIds,
        DateOnly from,
        DateOnly until,
        Guid? analyseToken,
        CancellationToken cancellationToken)
    {
        return await context.Set<ScheduleCommand>()
            .AsNoTracking()
            .Where(c => clientIds.Contains(c.ClientId)
                        && c.CurrentDate >= from
                        && c.CurrentDate <= until
                        && (c.AnalyseToken == analyseToken
                            || (c.AnalyseToken == null && analyseToken == null)))
            .ToListAsync(cancellationToken);
    }
}
