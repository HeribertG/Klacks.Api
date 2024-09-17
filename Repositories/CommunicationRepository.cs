using Klacks_api.Datas;
using Klacks_api.Interfaces;
using Klacks_api.Models.Settings;
using Klacks_api.Models.Staffs;
using Microsoft.EntityFrameworkCore;

namespace Klacks_api.Repositories
{
  public class CommunicationRepository : BaseRepository<Communication>, ICommunicationRepository

  {
    private readonly DataBaseContext context;

    public CommunicationRepository(DataBaseContext context)
      : base(context)
    {
      this.context = context;
    }

    public async Task<List<Communication>> GetClient(Guid id)
    {
      return await this.context.Communication.Where(c => c.ClientId == id).ToListAsync();
    }

    public async Task<List<CommunicationType>> TypeList()
    {
      return await this.context.CommunicationType.ToListAsync();
    }
  }
}
