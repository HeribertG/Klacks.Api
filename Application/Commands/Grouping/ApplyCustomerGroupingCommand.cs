// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Applies the geographic customer-grouping proposal: moves each customer to its nearest location
/// group and retires the coarser location memberships it replaces. Idempotent — re-running it after a
/// successful apply changes nothing.
/// </summary>
/// <param name="EntityType">Which client population to move into its location group.</param>
/// <param name="ValidFrom">Start date of the new memberships, as UTC midnight. Null means the company's
/// current date — the caller must not substitute today itself, because the resolved date is reported
/// back and a silently replaced date would be indistinguishable from an honoured one.</param>

using Klacks.Api.Application.DTOs.Grouping;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Grouping;

public record ApplyCustomerGroupingCommand(
    EntityTypeEnum EntityType = EntityTypeEnum.Customer,
    DateTime? ValidFrom = null)
    : IRequest<CustomerGroupingApplyResult>;
