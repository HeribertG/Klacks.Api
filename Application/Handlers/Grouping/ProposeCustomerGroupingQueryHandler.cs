// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for <see cref="ProposeCustomerGroupingQuery"/>. Delegates to the shared
/// <see cref="CustomerGroupingPlanner"/> to compute the read-only assignment proposal.
/// </summary>
/// <param name="planner">Computes the nearest-location-group proposal for all customers.</param>

using Klacks.Api.Application.DTOs.Grouping;
using Klacks.Api.Application.Queries.Grouping;
using Klacks.Api.Application.Services.Grouping;
using Klacks.Api.Application.Interfaces.Grouping;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Grouping;

public sealed class ProposeCustomerGroupingQueryHandler
    : IRequestHandler<ProposeCustomerGroupingQuery, CustomerGroupingProposal>
{
    private readonly ICustomerGroupingPlanner _planner;

    public ProposeCustomerGroupingQueryHandler(ICustomerGroupingPlanner planner)
    {
        _planner = planner;
    }

    public async Task<CustomerGroupingProposal> Handle(
        ProposeCustomerGroupingQuery request, CancellationToken cancellationToken)
    {
        return await _planner.BuildProposalAsync(request.EntityType, cancellationToken);
    }
}
