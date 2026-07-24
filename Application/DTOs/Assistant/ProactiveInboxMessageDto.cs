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
}
