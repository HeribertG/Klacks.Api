// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// What one detector run did: how many events it found, how many of them were handed to the notification
/// pipeline and how many open ledger rows of its kind were resolved, so the background tick can sum them per tick.
/// </summary>
/// <param name="Events">Events the detector returned.</param>
/// <param name="Dispatched">Events handed to IAgentTriggerService.OnEventAsync.</param>
/// <param name="Resolved">Open ledger rows of the detector's kind that were closed because the condition is gone.</param>

namespace Klacks.Api.Domain.Models.Assistant;

public sealed record AgentTriggerDetectorRunOutcome(int Events, int Dispatched, int Resolved);
