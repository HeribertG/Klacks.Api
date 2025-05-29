using Klacks.Api.Datas;
using Klacks.Api.Interfaces;
using Klacks.Api.Models.Settings;

namespace Klacks.Api.Repositories
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
