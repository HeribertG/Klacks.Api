using Klacks.Api.Datas;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Models.Associations;

namespace Klacks.Api.Infrastructure.Repositories;

public class MembershipRepository : BaseRepository<Membership>, IMembershipRepository
{
    private readonly DataBaseContext context;

    public MembershipRepository(DataBaseContext context, ILogger<Membership> logger)
      : base(context, logger)
    {
        this.context = context;
    }
}
