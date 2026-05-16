// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Per-user trigger preferences: mute a kind, snooze a kind until a UTC time, and set
/// a minimum-severity threshold. Used by AgentTriggerService to filter before dispatch.
/// </summary>

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IAgentTriggerPreferenceService
{
    bool IsAllowed(string userId, string triggerKind, string severity);

    AgentTriggerPreference GetPreference(string userId, string triggerKind);

    void Mute(string userId, string triggerKind, bool muted);

    void Snooze(string userId, string triggerKind, DateTime? snoozedUntilUtc);

    void SetMinimumSeverity(string userId, string triggerKind, string severity);
}

public sealed record AgentTriggerPreference(
    string UserId,
    string TriggerKind,
    bool Muted,
    DateTime? SnoozedUntilUtc,
    string MinimumSeverity);
