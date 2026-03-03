// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Staffs;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.ClientAvailabilities;

public record ListClientAvailabilitiesQuery(
    DateOnly StartDate,
    DateOnly EndDate) : IRequest<IEnumerable<ClientAvailabilityResource>>;
