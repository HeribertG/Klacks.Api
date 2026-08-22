// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for loading aggregated availability ranges per client and day.
/// Delegates to the SQL-function-backed schedule service, no own aggregation logic.
/// </summary>
/// <param name="request">Query with date range and client IDs</param>
using Klacks.Api.Application.DTOs.Staffs;
using Klacks.Api.Application.Queries.ClientAvailabilities;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Application.Handlers.ClientAvailabilities;

public class GetClientAvailabilityRangesQueryHandler : BaseHandler, IRequestHandler<GetClientAvailabilityRangesQuery, List<ClientAvailabilityRangeResource>>
{
    private readonly IClientAvailabilityScheduleService _scheduleService;

    public GetClientAvailabilityRangesQueryHandler(
        IClientAvailabilityScheduleService scheduleService,
        ILogger<GetClientAvailabilityRangesQueryHandler> logger)
        : base(logger)
    {
        _scheduleService = scheduleService;
    }

    public async Task<List<ClientAvailabilityRangeResource>> Handle(
        GetClientAvailabilityRangesQuery request,
        CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var entries = await _scheduleService
                .GetClientAvailabilityQuery(request.StartDate, request.EndDate, request.ClientIds)
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
