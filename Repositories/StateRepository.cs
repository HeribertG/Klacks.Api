using Klacks_api.Datas;
using Klacks_api.Interfaces;
using Klacks_api.Models.Settings;

namespace Klacks_api.Repositories
{
  public class StateRepository : BaseRepository<State>, IStateRepository
  {
    private readonly DataBaseContext context;

    public StateRepository(DataBaseContext context)
        : base(context)
    {
      this.context = context;
    }
  }
}
