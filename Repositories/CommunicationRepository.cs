using Klacks.Api.Datas;
using Klacks.Api.Interfaces;
using Klacks.Api.Models.Settings;
using Klacks.Api.Models.Staffs;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Repositories
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
