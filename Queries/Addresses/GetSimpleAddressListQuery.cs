using Klacks_api.Resources.Staffs;
using MediatR;

namespace Klacks_api.Queries.Addresses
{
  public record GetSimpleAddressListQuery(Guid Id) : IRequest<IEnumerable<AddressResource>>;
}
