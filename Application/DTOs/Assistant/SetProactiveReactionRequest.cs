// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Body of the set-reaction request on a proactive message.
/// </summary>
/// <param name="Reaction">Either "helpful" or "dismissed"; case-insensitive, anything else is rejected.</param>
/// <param name="RejectReason">
/// Optional, and only meaningful together with "dismissed": one of the AgentConditionRejectReason names
/// (generallyUnwanted, wrongThisTime, alreadyHandled, noReason), case-insensitive. Omitting it means the
/// user gave no reason, which the condition ledger stores as NoReason - so "dismissed with no reason" and
/// "dismissed before the reason picker existed" are the same request, on purpose. Sending one alongside
/// "helpful" is a client error and answered with 400 rather than silently dropped.
/// </param>

namespace Klacks.Api.Application.DTOs.Assistant;

public class SetProactiveReactionRequest
{
    public string Reaction { get; set; } = string.Empty;

    public string? RejectReason { get; set; }
}
