using Klacks.Api.Presentation.DTOs.Staffs;
using MediatR;

namespace Klacks.Api.Queries.Addresses
{
    public record GetSimpleAddressListQuery(Guid Id) : IRequest<IEnumerable<AddressResource>>;
}
