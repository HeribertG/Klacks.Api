// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Orders;

/// <summary>
/// An open order no group could be derived for, together with the reason it stays unassigned.
/// </summary>
/// <param name="OrderId">Id of the order that could not be placed.</param>
/// <param name="OrderName">Name of the order, for the preview text.</param>
/// <param name="CustomerName">Display name of the customer, or an empty string when the order has none.</param>
/// <param name="Reason">Why no group could be derived (no customer, no usable address, no matching group).</param>
public sealed record UnassignableOrder(
    Guid OrderId,
    string OrderName,
    string CustomerName,
    string Reason);
