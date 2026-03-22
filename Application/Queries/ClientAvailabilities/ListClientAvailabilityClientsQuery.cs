// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Query for loading the client list for client availability with filter.
/// </summary>
/// <param name="Filter">Filter with search string, group, paging</param>
using Klacks.Api.Domain.DTOs.Filter;
using Klacks.Api.Application.DTOs.Filter;
using Klacks.Api.Application.DTOs.Staffs;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.ClientAvailabilities;

public record ListClientAvailabilityClientsQuery(
    ClientAvailabilityClientFilter Filter) : IRequest<ClientAvailabilityClientListResponse>;
