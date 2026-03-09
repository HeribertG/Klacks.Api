// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Schedules;

public class ScheduleNoteRepository : BaseRepository<ScheduleNote>, IScheduleNoteRepository
{
    public ScheduleNoteRepository(DataBaseContext context, ILogger<ScheduleNote> logger)
        : base(context, logger)
    {
    }

    public override async Task<ScheduleNote?> Get(Guid id)
    {
        return await context.Set<ScheduleNote>()
            .FirstOrDefaultAsync(e => e.Id == id);
    }
}
