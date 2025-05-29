using Klacks.Api.Datas;
using Klacks.Api.Interfaces;
using Klacks.Api.Models.Schedules;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Repositories
{
    public class BreakRepository : BaseRepository<Break>, IBreakRepository
    {
        private readonly DataBaseContext context;

        public BreakRepository(DataBaseContext context)
          : base(context)
        {
            this.context = context;
        }

        public async Task<List<Break>> GetClientBreakList(Guid id)
        {
            return await this.context.Break.Include(c => c.Client).ThenInclude(m => m!.Membership).Where(x => x.ClientId == id).OrderBy(x => x.From).ToListAsync();
        }
    }
}
