// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Raised inside the transaction that places inbound planning commands when one add_schedule_commands_range call fails,
/// so the transaction rolls back the ranges already saved (all or nothing). Never leaves the orchestrator.
/// </summary>
/// <param name="keyword">Planning command whose placement failed</param>
/// <param name="reason">Message of the failed skill call</param>

namespace Klacks.Api.Infrastructure.Inbound;

internal sealed class InboundCommandPlacementFailedException(string keyword, string reason)
    : Exception($"Placing planning command {keyword} failed: {reason}")
{
    public string Keyword { get; } = keyword;

    public string Reason { get; } = reason;
}
