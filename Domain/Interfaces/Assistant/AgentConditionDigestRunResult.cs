// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Outcome of one RunIfDueAsync call, together with RecipientsNotified: how many planners had a
/// non-empty digest built and handed off to IAgentTriggerService.OnEventAsync. This counts hand-off, not
/// confirmed delivery - a planner's own mute preference, an existing dedup row, or an exhausted rate
/// limit inside OnEventAsync can still silently drop that planner's message afterwards.
/// </summary>

namespace Klacks.Api.Domain.Interfaces.Assistant;

public sealed record AgentConditionDigestRunResult(AgentConditionDigestOutcome Outcome, int RecipientsNotified);
