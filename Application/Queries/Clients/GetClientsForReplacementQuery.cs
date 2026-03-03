// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.DTOs.Staffs;

namespace Klacks.Api.Application.Queries.Clients;

public sealed record GetClientsForReplacementQuery() : IRequest<IEnumerable<ClientForReplacementResource>>;
