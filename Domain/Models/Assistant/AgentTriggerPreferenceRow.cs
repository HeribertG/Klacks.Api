// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Persistent per-user, per-kind trigger preference. Replaces the in-memory store from S8
/// so settings survive restarts and follow the user across sessions.
/// </summary>

using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Domain.Models.Assistant;

public class AgentTriggerPreferenceRow : BaseEntity
{
    public string UserId { get; set; } = string.Empty;

    public string TriggerKind { get; set; } = string.Empty;

    public bool Muted { get; set; }

    public DateTime? SnoozedUntilUtc { get; set; }

    public string MinimumSeverity { get; set; } = AgentTriggerSeverity.Low;
}
