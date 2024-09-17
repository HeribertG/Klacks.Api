using Klacks_api.Models.Staffs;

namespace Klacks_api.Interfaces;

public interface IAddressRepository : IBaseRepository<Address>
{
  Task<List<Address>> ClienList(Guid id);

  Task<List<Address>> SimpleList(Guid id);
}
