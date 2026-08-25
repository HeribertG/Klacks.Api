// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Assistant;

public record ProactiveInboxMessageDto
{
    public Guid Id { get; init; }

    public string Content { get; init; } = string.Empty;

    public IReadOnlyDictionary<string, string> ContentParams { get; init; } = new Dictionary<string, string>();

    public string Severity { get; init; } = string.Empty;

    public string Kind { get; init; } = string.Empty;

    public string? ActionRoute { get; init; }

    public IReadOnlyDictionary<string, string>? ActionParams { get; init; }

    public string Reaction { get; init; } = string.Empty;

    public DateTime? CreatedUtc { get; init; }

    public DateTime? ReadAtUtc { get; init; }

    /// <summary>
    /// Whether this message reported a condition-ledger finding, i.e. whether the "mach du" delegate
    /// action (Etappe 4e) has anything to act on. False for the majority of dispatch rows, which never
    /// carry a ConditionId - the client uses this to hide the delegate button rather than offer an
    /// action that would answer not-found.
    /// </summary>
    public bool CanDelegate { get; init; }
}
