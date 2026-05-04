// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Replaces the entire client sort order for the given user and group.
/// </summary>
/// <param name="UserId">Current user's identity ID (from JWT claim)</param>
/// <param name="GroupId">Group whose sort order is being saved</param>
/// <param name="Entries">Full ordered list — replaces all existing entries</param>

using Klacks.Api.Application.DTOs;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Schedule;

public record SaveClientSortOrderCommand(
    string UserId,
    Guid GroupId,
    List<ClientSortOrderDto> Entries) : IRequest<bool>;
