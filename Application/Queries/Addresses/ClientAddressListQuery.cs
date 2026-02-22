// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Staffs;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Addresses;

public record ClientAddressListQuery(Guid Id) : IRequest<IEnumerable<AddressResource>>;
