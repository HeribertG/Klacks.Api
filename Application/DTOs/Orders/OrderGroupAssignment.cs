// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Orders;

/// <summary>
/// One planned placement of an open order into a group, derived from the customer's address.
/// </summary>
/// <param name="OrderId">Id of the order that would be linked.</param>
/// <param name="OrderName">Name of the order, for the preview text.</param>
/// <param name="CustomerName">Display name of the customer whose address decided the placement.</param>
/// <param name="GroupId">Id of the group the order would be linked to.</param>
/// <param name="GroupName">Name of that group.</param>
/// <param name="MatchReason">How the group was found: city name, canton code or nearest coordinates, plus the address used.</param>
/// <param name="DistanceKm">Great-circle distance to the group anchor; only set for a coordinate match.</param>
public sealed record OrderGroupAssignment(
    Guid OrderId,
    string OrderName,
    string CustomerName,
    Guid GroupId,
    string GroupName,
    string MatchReason,
    double? DistanceKm);
