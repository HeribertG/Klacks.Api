// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Decides whether a proactive trigger event is pushed live into the chat of a CONNECTED user
/// (SignalR) instead of only landing in the inbox with a lightweight inbox-changed signal. This is
/// the cheap-interruption gate: the recipient is already sitting in front of Klacks, so every
/// companion event is admitted regardless of severity, plus any high-severity operational alert.
/// The offline messenger deliberately does not share this test - interrupting a phone at night is a
/// far costlier channel and gated separately by MessengerWakeUpPolicy. A user actively chatting is
/// never live-pushed at all; the inbox row still carries the message.
/// </summary>

using Klacks.Api.Domain.Interfaces.Assistant;

namespace Klacks.Api.Domain.Constants;

public static class ProactiveLivePushPolicy
{
    private static readonly TimeSpan ActiveConversationWindow = TimeSpan.FromMinutes(3);

    /// <summary>
    /// True when the event may be pushed live to this connected user right now: the user is not in
    /// an active conversation and the event qualifies as loud.
    /// </summary>
    public static bool ShouldLivePush(IAgentTriggerEvent triggerEvent, IUserActivityTracker activityTracker, string userId)
    {
        if (activityTracker.IsRecentlyActive(userId, ActiveConversationWindow))
        {
            return false;
        }

        return IsLoudEvent(triggerEvent);
    }

    /// <summary>
    /// Definition of "worth interrupting a CONNECTED user for", used by the SignalR live push only.
    /// Admits every companion event regardless of severity - right for a chat bubble next to a user
    /// who is already working, wrong for a phone at night (that gate is MessengerWakeUpPolicy).
    /// </summary>
    public static bool IsLoudEvent(IAgentTriggerEvent triggerEvent)
    {
        if (string.Equals(triggerEvent.Severity, AgentTriggerSeverity.High, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return IsCompanionEvent(triggerEvent);
    }

    private static bool IsCompanionEvent(IAgentTriggerEvent triggerEvent)
    {
        return !triggerEvent.PlannersOnly && !triggerEvent.AdminOnly;
    }
}
