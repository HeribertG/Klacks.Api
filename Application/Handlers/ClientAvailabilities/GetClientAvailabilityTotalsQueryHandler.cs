// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for loading total available hours and days with availability per client.
/// </summary>
/// <param name="request">Query with date range and client IDs</param>
using Klacks.Api.Application.DTOs.Staffs;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.ClientAvailabilities;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.ClientAvailabilities;

public class GetClientAvailabilityTotalsQueryHandler : BaseHandler, IRequestHandler<GetClientAvailabilityTotalsQuery, List<ClientAvailabilityTotalResource>>
{
    private readonly IClientAvailabilityRepository _repository;

    public GetClientAvailabilityTotalsQueryHandler(
        IClientAvailabilityRepository repository,
        ILogger<GetClientAvailabilityTotalsQueryHandler> logger)
        : base(logger)
    {
        _repository = repository;
    }

    public async Task<List<ClientAvailabilityTotalResource>> Handle(
        GetClientAvailabilityTotalsQuery request,
        CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            return await _repository.GetTotalsByClientsAndDateRange(
                request.ClientIds, request.StartDate, request.EndDate);
        }, "GetClientAvailabilityTotals", new { request.StartDate, request.EndDate });
    }
}
