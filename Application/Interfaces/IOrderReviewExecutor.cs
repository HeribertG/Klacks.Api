// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Orders.Review;

namespace Klacks.Api.Application.Interfaces;

/// <summary>
/// Carries out the review decisions for one order in ONE transaction: change the order (start, group, required
/// qualifications, default expenses), seal it into its plannable shift and cut that into the decided parts. Any failure
/// rolls the whole order back and is reported, never thrown.
/// </summary>
public interface IOrderReviewExecutor
{
    Task<OrderReviewExecutionOutcome> ExecuteAsync(
        OrderReviewExecutionTarget target, string userName, string? language, CancellationToken cancellationToken = default);
}
