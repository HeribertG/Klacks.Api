// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Handler for loading total available hours and days with availability per client.
/// Requested clients outside the caller's group visibility are dropped like unknown ids.
/// </summary>
/// <param name="request">Query with date range and client IDs</param>
/// <param name="clientVisibilityGuard">Reduces the requested client ids to those the calling user may see</param>
using Klacks.Api.Application.DTOs.Staffs;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.ClientAvailabilities;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.ClientAvailabilities;

public class GetClientAvailabilityTotalsQueryHandler : BaseHandler, IRequestHandler<GetClientAvailabilityTotalsQuery, List<ClientAvailabilityTotalResource>>
{
    private readonly IClientAvailabilityRepository _repository;
    private readonly IClientVisibilityGuard _clientVisibilityGuard;

    public GetClientAvailabilityTotalsQueryHandler(
        IClientAvailabilityRepository repository,
        IClientVisibilityGuard clientVisibilityGuard,
        ILogger<GetClientAvailabilityTotalsQueryHandler> logger)
        : base(logger)
    {
        _repository = repository;
        _clientVisibilityGuard = clientVisibilityGuard;
    }

    public async Task<List<ClientAvailabilityTotalResource>> Handle(
        GetClientAvailabilityTotalsQuery request,
        CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var visibleClientIds = await _clientVisibilityGuard.FilterVisibleAsync(
                request.ClientIds, id => id, cancellationToken);

            return await _repository.GetTotalsByClientsAndDateRange(
                visibleClientIds, request.StartDate, request.EndDate);
        }, "GetClientAvailabilityTotals", new { request.StartDate, request.EndDate });
    }
}
