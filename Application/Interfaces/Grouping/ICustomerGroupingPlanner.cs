// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Grouping;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.Interfaces.Grouping;

public interface ICustomerGroupingPlanner
{
    Task<CustomerGroupingProposal> BuildProposalAsync(
        EntityTypeEnum entityType = EntityTypeEnum.Customer,
        CancellationToken cancellationToken = default);
}
