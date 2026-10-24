// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Orders.Review;
using Klacks.Api.Domain.Models.Assistant.OrderReview;

namespace Klacks.Api.Application.Interfaces;

/// <summary>
/// Applies one parsed set_order_review_decision call to the targeted items of the review draft (in memory; the caller
/// stores the draft). Validates every answer - start only for full-day orders, a split that tiles the order and that
/// the members' rules allow at least for some, a group only for an order without one and only with full access,
/// known qualifications - and refuses what does not fit instead of storing it.
/// </summary>
public interface IOrderReviewDecisionApplier
{
    Task<OrderReviewDecisionOutcome> ApplyAsync(
        OrderReviewDraft draft,
        IReadOnlyList<OrderReviewDraftItem> targets,
        OrderReviewDecision decision,
        OrderReviewScope scope,
        string? language,
        CancellationToken cancellationToken = default);
}
