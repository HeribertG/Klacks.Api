// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Orders;

/// <summary>
/// An open order that cannot be sealed yet because required fields are missing or invalid.
/// </summary>
/// <param name="OrderId">Id of the order that stays unsealed.</param>
/// <param name="OrderName">Name of the order, for the report text.</param>
/// <param name="MissingRequirements">Field names that block the sealing, e.g. 'at least one group'.</param>
public sealed record BlockedOrder(Guid OrderId, string OrderName, IReadOnlyList<string> MissingRequirements);
