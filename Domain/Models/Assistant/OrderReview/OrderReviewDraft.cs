// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The open-orders review the assistant goes through with a planner across dialog turns: one item per open order with
/// the server's proposals, the user's decisions and what became of the order. Held in the pending draft store keyed by
/// user and conversation; each Set slides ExpiresAtUtc forward by the store TTL. Nothing changes in the domain until
/// execute_order_review runs as the confirmed replay of its preview.
/// </summary>
namespace Klacks.Api.Domain.Models.Assistant.OrderReview;

public sealed class OrderReviewDraft
{
    public List<OrderReviewDraftItem> Items { get; set; } = [];

    public DateTime ExpiresAtUtc { get; set; }
}
