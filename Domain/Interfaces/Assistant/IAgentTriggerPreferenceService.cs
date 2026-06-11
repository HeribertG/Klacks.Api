// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Per-user trigger preferences: mute a kind, snooze a kind until a UTC time, and set
/// a minimum-severity threshold. Used by AgentTriggerService to filter before dispatch.
/// </summary>

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IAgentTriggerPreferenceService
{
    Task<bool> IsAllowedAsync(string userId, string triggerKind, string severity);

    Task<AgentTriggerPreference> GetPreferenceAsync(string userId, string triggerKind);

    Task MuteAsync(string userId, string triggerKind, bool muted);

    Task SnoozeAsync(string userId, string triggerKind, DateTime? snoozedUntilUtc);

    Task SetMinimumSeverityAsync(string userId, string triggerKind, string severity);
}

public sealed record AgentTriggerPreference(
    string UserId,
    string TriggerKind,
    bool Muted,
    DateTime? SnoozedUntilUtc,
    string MinimumSeverity);
