// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler resolving the current user's dashboard visibility status.
/// </summary>
/// <param name="groupVisibilityService">Service for determining the user's group visibility scope</param>
using Klacks.Api.Application.DTOs.Dashboard;
using Klacks.Api.Application.Queries.Dashboard;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Handlers.Dashboard;

public class GetDashboardVisibilityStatusQueryHandler : BaseHandler, IRequestHandler<GetDashboardVisibilityStatusQuery, DashboardVisibilityStatusResource>
{
    private readonly IGroupVisibilityService _groupVisibilityService;

    public GetDashboardVisibilityStatusQueryHandler(
        IGroupVisibilityService groupVisibilityService,
        ILogger<GetDashboardVisibilityStatusQueryHandler> logger)
        : base(logger)
    {
        _groupVisibilityService = groupVisibilityService;
    }

    public async Task<DashboardVisibilityStatusResource> Handle(GetDashboardVisibilityStatusQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var scope = await _groupVisibilityService.GetVisibilityScopeAsync();

            return new DashboardVisibilityStatusResource
            {
                IsRestricted = !scope.IsUnrestricted,
                HasVisibleGroups = scope.HasVisibleGroups
            };
        }, nameof(Handle));
    }
}
