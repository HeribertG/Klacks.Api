using Klacks.Api.Datas;
using Klacks.Api.Interfaces;
using Klacks.Api.Models.Schedules;

namespace Klacks.Api.Repositories;

public class WorkRepository : BaseRepository<Work>, IWorkRepository
{
    private readonly DataBaseContext context;

    public WorkRepository(DataBaseContext context)
        : base(context)
    {
        this.context = context;
    }
}
