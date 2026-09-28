// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Grouping;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.Interfaces.Grouping;

public interface ICustomerGroupingPlanner
{
    Task<CustomerGroupingProposal> BuildProposalAsync(
        EntityTypeEnum entityType = EntityTypeEnum.Customer,
        CancellationToken cancellationToken = default);
}
