using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Assistant;

public class DeleteAgentMemoryCommand : IRequest<Unit>
{
    public Guid AgentId { get; set; }
    public Guid MemoryId { get; set; }
}
