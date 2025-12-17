using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Persistence;

namespace Klacks.Api.Infrastructure.Repositories;

public class WorkChangeRepository : BaseRepository<WorkChange>, IWorkChangeRepository
{
    public WorkChangeRepository(DataBaseContext context, ILogger<WorkChange> logger)
        : base(context, logger)
    {
    }
}
