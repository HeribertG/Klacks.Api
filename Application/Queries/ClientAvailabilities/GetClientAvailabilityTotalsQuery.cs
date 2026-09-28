// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Query for loading total available hours and days with availability per client.
/// </summary>
/// <param name="StartDate">Start of the date range (inclusive)</param>
/// <param name="EndDate">End of the date range (inclusive)</param>
/// <param name="ClientIds">Clients to include</param>
using Klacks.Api.Application.DTOs.Staffs;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.ClientAvailabilities;

public record GetClientAvailabilityTotalsQuery(
    DateOnly StartDate,
    DateOnly EndDate,
    List<Guid> ClientIds) : IRequest<List<ClientAvailabilityTotalResource>>;
