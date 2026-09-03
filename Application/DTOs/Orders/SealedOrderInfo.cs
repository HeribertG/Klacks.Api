// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Orders;

/// <summary>
/// An order that was sealed, together with the plannable shift the transition created for it.
/// </summary>
/// <param name="OrderId">Id of the now immutable sealed order.</param>
/// <param name="OrderName">Name of the order.</param>
/// <param name="PlannableShiftId">Id of the OriginalShift created by the sealing.</param>
public sealed record SealedOrderInfo(Guid OrderId, string OrderName, Guid PlannableShiftId);
