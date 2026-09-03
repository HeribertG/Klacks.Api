// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Orders;

/// <summary>
/// An open order whose sealing was attempted and failed; its own transaction was rolled back while the
/// rest of the batch continued.
/// </summary>
/// <param name="OrderId">Id of the order whose sealing failed.</param>
/// <param name="OrderName">Name of the order, for the report text.</param>
/// <param name="Reason">Message of the refusal or exception that ended the attempt.</param>
public sealed record FailedOrder(Guid OrderId, string OrderName, string Reason);
