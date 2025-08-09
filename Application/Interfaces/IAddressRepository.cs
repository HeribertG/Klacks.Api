using Klacks.Api.Models.Staffs;

namespace Klacks.Api.Application.Interfaces;

public interface IAddressRepository : IBaseRepository<Address>
{
    Task<List<Address>> ClienList(Guid id);

    Task<List<Address>> SimpleList(Guid id);
}
