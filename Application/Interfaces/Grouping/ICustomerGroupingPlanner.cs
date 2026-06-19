// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Grouping;

namespace Klacks.Api.Application.Interfaces.Grouping;

public interface ICustomerGroupingPlanner
{
    Task<CustomerGroupingProposal> BuildProposalAsync(CancellationToken cancellationToken = default);
}
