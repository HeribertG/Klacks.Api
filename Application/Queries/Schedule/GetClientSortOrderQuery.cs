// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Returns the stored client sort order for the given user and group.
/// </summary>
/// <param name="UserId">Current user's identity ID (from JWT claim)</param>
/// <param name="GroupId">Group whose sort order is requested</param>

using Klacks.Api.Application.DTOs;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Schedule;

public record GetClientSortOrderQuery(string UserId, Guid GroupId) : IRequest<List<ClientSortOrderDto>>;
