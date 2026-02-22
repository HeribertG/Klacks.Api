// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Assistant;

public class ToggleMemoryPinCommand : IRequest<object?>
{
    public Guid AgentId { get; set; }
    public Guid MemoryId { get; set; }
}
