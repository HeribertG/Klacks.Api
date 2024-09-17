using Klacks_api.Datas;
using Klacks_api.Interfaces;
using Klacks_api.Models.Schedules;

namespace Klacks_api.Repositories;

public class ShiftRepository : BaseRepository<Shift>, IShiftRepository
{
  private readonly DataBaseContext context;

  public ShiftRepository(DataBaseContext context)
      : base(context)
  {
    this.context = context;
  }
}
