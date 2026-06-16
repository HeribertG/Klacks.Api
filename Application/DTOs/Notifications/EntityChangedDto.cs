// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Notifications;

/// <summary>
/// Pushed to the originating user over SignalR after Klacksy successfully executes a
/// mutating (non read-only) skill, so an open page can reload the affected data.
/// </summary>
public record EntityChangedDto
{
    /// <summary>Normalised lower-case names of the domain entities the skill changed (e.g. "client", "address").</summary>
    public IReadOnlyList<string> EntityTypes { get; init; } = Array.Empty<string>();

    /// <summary>The mutation kind: "create", "update" or "delete".</summary>
    public string Operation { get; init; } = string.Empty;

    /// <summary>The skill that produced the change (diagnostics and fine-grained client matching).</summary>
    public string SkillName { get; init; } = string.Empty;

    public DateTime Timestamp { get; init; }
}
