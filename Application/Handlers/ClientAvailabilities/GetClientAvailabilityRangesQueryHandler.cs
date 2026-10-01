// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Handler for loading aggregated availability ranges per client and day.
/// Delegates to the SQL-function-backed schedule service, no own aggregation logic.
/// Requested clients outside the caller's group visibility are dropped like unknown ids.
/// </summary>
/// <param name="request">Query with date range and client IDs</param>
/// <param name="clientVisibilityGuard">Reduces the requested client ids to those the calling user may see</param>
using Klacks.Api.Application.DTOs.Staffs;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.ClientAvailabilities;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Application.Handlers.ClientAvailabilities;

public class GetClientAvailabilityRangesQueryHandler : BaseHandler, IRequestHandler<GetClientAvailabilityRangesQuery, List<ClientAvailabilityRangeResource>>
{
    private readonly IClientAvailabilityScheduleService _scheduleService;
    private readonly IClientVisibilityGuard _clientVisibilityGuard;

    public GetClientAvailabilityRangesQueryHandler(
        IClientAvailabilityScheduleService scheduleService,
        IClientVisibilityGuard clientVisibilityGuard,
        ILogger<GetClientAvailabilityRangesQueryHandler> logger)
        : base(logger)
    {
        _scheduleService = scheduleService;
        _clientVisibilityGuard = clientVisibilityGuard;
    }

    public async Task<List<ClientAvailabilityRangeResource>> Handle(
        GetClientAvailabilityRangesQuery request,
        CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var visibleClientIds = await _clientVisibilityGuard.FilterVisibleAsync(
                request.ClientIds, id => id, cancellationToken);

            var entries = await _scheduleService
                .GetClientAvailabilityQuery(request.StartDate, request.EndDate, visibleClientIds)
                .ToListAsync(cancellationToken);

            return entries.Select(MapToResource).ToList();
        }, "GetClientAvailabilityRanges", new { request.StartDate, request.EndDate });
    }

    private static ClientAvailabilityRangeResource MapToResource(ClientAvailabilityScheduleEntry entry)
    {
        return new ClientAvailabilityRangeResource
        {
            ClientId = entry.ClientId,
            Date = DateOnly.FromDateTime(entry.AvailabilityDate),
            Ranges = entry.AvailabilityRanges
        };
    }
}
