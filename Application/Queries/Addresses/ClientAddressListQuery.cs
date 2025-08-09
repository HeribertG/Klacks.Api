using Klacks.Api.Presentation.DTOs.Staffs;
using MediatR;

namespace Klacks.Api.Application.Queries.Addresses;

public record ClientAddressListQuery(Guid Id) : IRequest<IEnumerable<AddressResource>>;
