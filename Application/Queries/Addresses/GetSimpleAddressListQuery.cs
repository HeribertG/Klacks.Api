using Klacks.Api.Application.DTOs.Staffs;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Addresses
{
    public record GetSimpleAddressListQuery(Guid Id) : IRequest<IEnumerable<AddressResource>>;
}
