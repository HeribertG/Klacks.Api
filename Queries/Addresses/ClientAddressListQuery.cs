using Klacks.Api.Presentation.Resources.Staffs;
using MediatR;

namespace Klacks.Api.Queries.Addresses;

public record ClientAddressListQuery(Guid Id) : IRequest<IEnumerable<AddressResource>>;
