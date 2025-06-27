using Klacks.Api.Resources.Staffs;
using MediatR;

namespace Klacks.Api.Queries.Addresses;

public record ClientAddressListQuery(Guid Id) : IRequest<IEnumerable<AddressResource>>;
