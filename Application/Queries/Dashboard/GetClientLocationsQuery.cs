// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Dashboard;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Dashboard;

public record GetClientLocationsQuery : IRequest<IEnumerable<ClientLocationResource>>;
