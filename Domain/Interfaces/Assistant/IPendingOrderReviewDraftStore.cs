// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Short-lived store for an order-review draft being collected across dialog turns: Set persists the
/// draft for a user/conversation, Get returns the outstanding draft (or null once it has expired) and
/// Clear removes it once the review is finished or cancelled. HasDraft tells whether an unexpired draft exists without
/// loading it (asked on every chat turn by the toolset policy). Entries are scoped per (user, conversation).
/// </summary>

using Klacks.Api.Domain.Models.Assistant.OrderReview;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IPendingOrderReviewDraftStore
{
    void Set(Guid userId, string conversationId, OrderReviewDraft draft);

    OrderReviewDraft? Get(Guid userId, string conversationId);

    bool HasDraft(Guid userId, string conversationId);

    void Clear(Guid userId, string conversationId);
}
