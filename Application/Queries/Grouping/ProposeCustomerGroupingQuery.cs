// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Requests the geographic customer-grouping proposal (dry run): which customers would move to which
/// nearest location group, plus the customers that cannot be assigned. Read-only — does not persist.
/// </summary>

using Klacks.Api.Application.DTOs.Grouping;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Grouping;

public record ProposeCustomerGroupingQuery(EntityTypeEnum EntityType = EntityTypeEnum.Customer)
    : IRequest<CustomerGroupingProposal>;
