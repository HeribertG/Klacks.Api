// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Orders;

/// <summary>
/// Result of trying to seal one single order, as returned by <c>IOrderSealingService.SealAsync</c>.
/// Exactly one of PlannableShiftId (success) and FailureReason (refusal or failure) is set.
/// </summary>
/// <param name="OrderId">Id of the order the attempt was made for.</param>
/// <param name="OrderName">Name of the order, carried so callers can report without re-reading it.</param>
/// <param name="IsSealed">True when the order is now a SealedOrder and its plannable shift exists.</param>
/// <param name="PlannableShiftId">Id of the OriginalShift created by the sealing transition; null when it failed.</param>
/// <param name="FailureReason">Human-readable reason the order was not sealed; null on success.</param>
/// <param name="MissingRequirements">Field names that block sealing; empty when the order was sealable.</param>
public sealed record OrderSealingOutcome(
    Guid OrderId,
    string OrderName,
    bool IsSealed,
    Guid? PlannableShiftId,
    string? FailureReason,
    IReadOnlyList<string> MissingRequirements);
