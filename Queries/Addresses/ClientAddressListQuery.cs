using Klacks.Api.Presentation.DTOs.Staffs;
using MediatR;

namespace Klacks.Api.Queries.Addresses;

public record ClientAddressListQuery(Guid Id) : IRequest<IEnumerable<AddressResource>>;
