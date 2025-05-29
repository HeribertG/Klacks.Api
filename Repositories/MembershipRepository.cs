using Klacks.Api.Datas;
using Klacks.Api.Interfaces;
using Klacks.Api.Models.Associations;

namespace Klacks.Api.Repositories
{
    public class MembershipRepository : BaseRepository<Membership>, IMembershipRepository
    {
        private readonly DataBaseContext context;

        public MembershipRepository(DataBaseContext context)
          : base(context)
        {
            this.context = context;
        }
    }
}
