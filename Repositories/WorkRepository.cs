using Klacks.Api.Datas;
using Klacks.Api.Interfaces;
using Klacks.Api.Models.Schedules;

namespace Klacks.Api.Repositories;

public class WorkRepository : BaseRepository<Work>, IWorkRepository
{
    public WorkRepository(DataBaseContext context, ILogger<Work> logger)
        : base(context, logger)
    {
    }
}
