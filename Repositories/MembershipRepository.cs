using Klacks_api.Datas;
using Klacks_api.Interfaces;
using Klacks_api.Models.Associations;

namespace Klacks_api.Repositories
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
