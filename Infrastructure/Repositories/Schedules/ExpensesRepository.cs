// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Schedules;

public class ExpensesRepository : BaseRepository<Expenses>, IExpensesRepository
{
    public ExpensesRepository(DataBaseContext context, ILogger<Expenses> logger)
        : base(context, logger)
    {
    }

    public override async Task<Expenses?> Get(Guid id)
    {
        return await context.Set<Expenses>()
            .Include(e => e.Work)
            .FirstOrDefaultAsync(e => e.Id == id);
    }
}
