using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Staffs;

namespace Klacks.Api.Application.Interfaces;

public interface IAddressRepository : IBaseRepository<Address>
{
    Task<List<Address>> ClienList(Guid id);

    Task<List<Address>> SimpleList(Guid id);
}
