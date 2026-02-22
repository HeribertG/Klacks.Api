using Klacks.Api.Application.Commands.Assistant;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Assistant;

public class DeleteAgentMemoryCommandHandler : IRequestHandler<DeleteAgentMemoryCommand, Unit>
{
    private readonly IAgentMemoryRepository _memoryRepository;

    public DeleteAgentMemoryCommandHandler(IAgentMemoryRepository memoryRepository)
    {
        _memoryRepository = memoryRepository;
    }

    public async Task<Unit> Handle(DeleteAgentMemoryCommand request, CancellationToken cancellationToken)
    {
        var memory = await _memoryRepository.GetByIdAsync(request.MemoryId, cancellationToken);
        if (memory == null || memory.AgentId != request.AgentId)
            throw new KeyNotFoundException($"Memory {request.MemoryId} not found for agent {request.AgentId}");

        await _memoryRepository.DeleteAsync(request.MemoryId, cancellationToken);
        return Unit.Value;
    }
}
