using Klacks_api.Datas;
using Klacks_api.Interfaces;
using Klacks_api.Models.Schedules;

namespace Klacks_api.Repositories;

public class WorkRepository : BaseRepository<Work>, IWorkRepository
{
  private readonly DataBaseContext context;

  public WorkRepository(DataBaseContext context)
      : base(context)
  {
    this.context = context;
  }
}
