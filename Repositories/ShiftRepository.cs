using Klacks.Api.Datas;
using Klacks.Api.Interfaces;
using Klacks.Api.Models.Schedules;

namespace Klacks.Api.Repositories;

public class ShiftRepository : BaseRepository<Shift>, IShiftRepository
{
  private readonly DataBaseContext context;

  public ShiftRepository(DataBaseContext context)
      : base(context)
  {
    this.context = context;
  }
}
