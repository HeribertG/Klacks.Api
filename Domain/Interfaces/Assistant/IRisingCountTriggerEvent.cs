// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Interfaces.Assistant;

/// <summary>
/// An event that may be delivered to the same recipient again under the same DedupKey, but only when its count has
/// RISEN above the highest count that recipient already received under that key (owner decision 2026-10-22 for the
/// daily open-order summary: new orders after the morning message produce a new message, fewer or the same do not).
/// AgentTriggerService stores the delivered count in the dispatch row's dedup key (RisingCountDedupKey) and compares
/// against it per recipient; the event's own DedupKey - and with it the condition-ledger fingerprint - stays unchanged.
/// </summary>
public interface IRisingCountTriggerEvent : IAgentTriggerEvent
{
    /// <summary>The count of this recipient's variant; must exceed the delivered high-water mark to be sent.</summary>
    int RisingCount { get; }
}
