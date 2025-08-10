using Klacks.Api.Domain.Common;
using Klacks.Api.Infrastructure.Persistence;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Infrastructure.Repositories;

public class WorkRepository : BaseRepository<Work>, IWorkRepository
{
    public WorkRepository(DataBaseContext context, ILogger<Work> logger)
        : base(context, logger)
    {
    }
}
