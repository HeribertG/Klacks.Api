// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.DTOs.Staffs;

namespace Klacks.Api.Application.Queries.Clients;

public sealed record FindListQuery(string? Company = null, string? Name = null, string? FirstName = null) : IRequest<IEnumerable<ClientResource>>;
