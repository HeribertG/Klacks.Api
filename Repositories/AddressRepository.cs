using Klacks.Api.Datas;
using Klacks.Api.Interfaces;
using Klacks.Api.Models.Staffs;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Repositories
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

        Task<List<Address>> IAddressRepository.ClienList(Guid id)
        {
            throw new NotImplementedException();
        }

        Task<List<Address>> IAddressRepository.SimpleList(Guid id)
        {
            throw new NotImplementedException();
        }
    }
}
