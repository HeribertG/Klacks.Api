// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Repository for ScheduleCommand entity with custom Get override.
/// </summary>
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Schedules;

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
}
