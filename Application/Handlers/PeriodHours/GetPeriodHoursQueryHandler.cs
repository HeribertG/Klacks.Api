// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Returns scheduled, surcharge and guaranteed hours per requested client for one period. Requested clients
/// outside the caller's group visibility are dropped like unknown ids and are absent from the result.
/// </summary>
/// <param name="clientVisibilityGuard">Reduces the requested client ids to those the calling user may see</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.PeriodHours;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Domain.DTOs.Schedules;

namespace Klacks.Api.Application.Handlers.PeriodHours;

public class GetPeriodHoursQueryHandler : IRequestHandler<GetPeriodHoursQuery, Dictionary<Guid, PeriodHoursResource>>
{
    private readonly IPeriodHoursService _periodHoursService;
    private readonly IClientVisibilityGuard _clientVisibilityGuard;

    public GetPeriodHoursQueryHandler(
        IPeriodHoursService periodHoursService,
        IClientVisibilityGuard clientVisibilityGuard)
    {
        _periodHoursService = periodHoursService;
        _clientVisibilityGuard = clientVisibilityGuard;
    }

    public async Task<Dictionary<Guid, PeriodHoursResource>> Handle(GetPeriodHoursQuery query, CancellationToken cancellationToken)
    {
        var visibleClientIds = await _clientVisibilityGuard.FilterVisibleAsync(
            query.Request.ClientIds, id => id, cancellationToken);

        return await _periodHoursService.GetPeriodHoursAsync(
            visibleClientIds,
            query.Request.StartDate,
            query.Request.EndDate,
            query.Request.AnalyseToken);
    }
}
