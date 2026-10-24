// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Orders.Review;

/// <summary>
/// What became of one order in a confirmed execute_order_review: changed, sealed and cut (with the plannable shift and
/// the number of parts), or failed with the reason - then its transaction was rolled back and the order is unchanged.
/// </summary>
/// <param name="OrderId">The order</param>
/// <param name="Succeeded">True when change, seal and cut were all written and verified</param>
/// <param name="PlannableShiftId">The plannable shift created by the sealing; null on failure</param>
/// <param name="PartCount">Parts of the cut; 0 when the order is not split or on failure</param>
/// <param name="FailureReason">Why the order failed; null on success</param>
public sealed record OrderReviewExecutionOutcome(
    Guid OrderId, bool Succeeded, Guid? PlannableShiftId, int PartCount, string? FailureReason);
