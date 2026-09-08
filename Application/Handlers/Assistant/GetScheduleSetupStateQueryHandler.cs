// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Reads the installation-wide setup snapshot from IScheduleActivityProbe and maps it onto the
/// HTTP-facing resource, so the domain record never travels outside the Application layer.
/// </summary>
/// <param name="activityProbe">Installation-wide setup snapshot along the order -> shift -> assignment chain.</param>

using Klacks.Api.Application.DTOs.Assistant;
using Klacks.Api.Application.Queries.Assistant;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Assistant;

public class GetScheduleSetupStateQueryHandler : IRequestHandler<GetScheduleSetupStateQuery, ScheduleSetupStateResource>
{
    private readonly IScheduleActivityProbe _activityProbe;

    public GetScheduleSetupStateQueryHandler(IScheduleActivityProbe activityProbe)
    {
        _activityProbe = activityProbe;
    }

    public async Task<ScheduleSetupStateResource> Handle(GetScheduleSetupStateQuery request, CancellationToken cancellationToken)
    {
        var state = await _activityProbe.GetSetupStateAsync(cancellationToken);

        return new ScheduleSetupStateResource
        {
            HasOrders = state.HasOrders,
            HasShifts = state.HasShifts,
            HasWork = state.HasWork,
            HasCustomers = state.HasCustomers,
            HasGroups = state.HasGroups,
        };
    }
}
