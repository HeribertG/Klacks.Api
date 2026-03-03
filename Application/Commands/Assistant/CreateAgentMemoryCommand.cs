// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Assistant;

public class CreateAgentMemoryCommand : IRequest<object>
{
    public Guid AgentId { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? Category { get; set; }
    public int? Importance { get; set; }
    public bool? IsPinned { get; set; }
    public DateTime? ExpiresAt { get; set; }
}
