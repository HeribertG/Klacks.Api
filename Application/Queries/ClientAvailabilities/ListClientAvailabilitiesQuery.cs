// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Staffs;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.ClientAvailabilities;

public record ListClientAvailabilitiesQuery(
    DateOnly StartDate,
    DateOnly EndDate) : IRequest<IEnumerable<ClientAvailabilityResource>>;
