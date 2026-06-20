// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Records that a proactive trigger with a given content key was already dispatched to a user, so the
/// same alert is never sent twice (survives restarts). Keyed by (UserId, TriggerKind, DedupKey).
/// </summary>

using Klacks.Api.Domain.Common;

namespace Klacks.Api.Domain.Models.Assistant;

public class ProactiveTriggerDispatchRow : BaseEntity
{
    public string UserId { get; set; } = string.Empty;

    public string TriggerKind { get; set; } = string.Empty;

    public string DedupKey { get; set; } = string.Empty;
}
