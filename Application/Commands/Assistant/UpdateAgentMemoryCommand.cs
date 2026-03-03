// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Assistant;

public class UpdateAgentMemoryCommand : IRequest<object?>
{
    public Guid AgentId { get; set; }
    public Guid MemoryId { get; set; }
    public string? Key { get; set; }
    public string? Content { get; set; }
    public string? Category { get; set; }
    public int? Importance { get; set; }
    public bool? IsPinned { get; set; }
}
