// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Loads the detail preview of a single sealed order (all work entries regardless of lock
/// level, with period-closed flags). Returns null when the id is not a sealed order.
/// @param orderId - Identifier of the sealed order shift (Status = SealedOrder)
/// @param fromDate - Optional lower bound for Work.CurrentDate
/// @param untilDate - Optional upper bound for Work.CurrentDate
/// </summary>
using Klacks.Api.Application.DTOs.Exports;

namespace Klacks.Api.Application.Interfaces.Exports;

public interface ISealedOrderDetailsLoader
{
    Task<SealedOrderDetailsResource?> LoadAsync(
        Guid orderId,
        DateOnly? fromDate,
        DateOnly? untilDate,
        CancellationToken cancellationToken = default);
}
