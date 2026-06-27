// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Applies the geographic customer-grouping proposal: moves each customer to its nearest location
/// group and retires the coarser location memberships it replaces. Idempotent — re-running it after a
/// successful apply changes nothing.
/// </summary>

using Klacks.Api.Application.DTOs.Grouping;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Grouping;

public record ApplyCustomerGroupingCommand(EntityTypeEnum EntityType = EntityTypeEnum.Customer)
    : IRequest<CustomerGroupingApplyResult>;
