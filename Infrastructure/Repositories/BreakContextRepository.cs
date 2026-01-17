using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories;

public class BreakContextRepository : BaseRepository<BreakContext>, IBreakContextRepository
{
    private readonly DataBaseContext context;

    public BreakContextRepository(DataBaseContext context, ILogger<BreakContext> logger)
        : base(context, logger)
    {
        this.context = context;
    }

    public async Task SoftDeleteByAbsenceIdAsync(Guid absenceId)
    {
        var breakContexts = await context.BreakContext
            .Where(bc => bc.AbsenceId == absenceId && !bc.IsDeleted)
            .ToListAsync();

        foreach (var breakContext in breakContexts)
        {
            context.Remove(breakContext);
        }
    }
}
