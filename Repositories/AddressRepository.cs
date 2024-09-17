using Klacks_api.Datas;
using Klacks_api.Interfaces;
using Klacks_api.Models.Staffs;
using Microsoft.EntityFrameworkCore;

namespace Klacks_api.Repositories
{
  public class AddressRepository : BaseRepository<Address>, IAddressRepository
  {
    private readonly DataBaseContext context;

    public AddressRepository(DataBaseContext context)
      : base(context)
    {
      this.context = context;
    }

    public async Task<List<Address>> ClienList(Guid id)
    {
      return await this.context.Address.IgnoreQueryFilters().Where(x => x.ClientId == id).OrderByDescending(x => x.ValidFrom).ToListAsync();
    }

    public async Task<List<Address>> SimpleList(Guid id)
    {
      return await this.context.Address.Where(c => c.ClientId == id).ToListAsync();
    }
  }
}
