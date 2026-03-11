// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Query zum Laden der Client-Liste für Client-Availability mit Filter.
/// </summary>
/// <param name="Filter">Filter mit SearchString, Gruppe, Paging</param>
using Klacks.Api.Application.DTOs.Filter;
using Klacks.Api.Application.DTOs.Staffs;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.ClientAvailabilities;

public record ListClientAvailabilityClientsQuery(
    ClientAvailabilityClientFilter Filter) : IRequest<ClientAvailabilityClientListResponse>;
