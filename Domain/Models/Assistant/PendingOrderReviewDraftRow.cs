// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Persistent row for an order-review draft kept across dialog turns, so a review started with start_order_review
/// survives a backend restart, and so does what execute_order_review already did per order (the review can resume
/// after a failure). Deliberately not a <c>BaseEntity</c>: the row is ephemeral and is hard-deleted on Clear/expiry
/// rather than soft-deleted.
/// </summary>
namespace Klacks.Api.Domain.Models.Assistant;

public sealed class PendingOrderReviewDraftRow
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string ConversationId { get; set; } = string.Empty;

    public string DraftJson { get; set; } = string.Empty;

    public DateTime ExpiresAtUtc { get; set; }
}
