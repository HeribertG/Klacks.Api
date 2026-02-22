using Klacks.Api.Application.Commands.Assistant;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Assistant;

public class ToggleMemoryPinCommandHandler : IRequestHandler<ToggleMemoryPinCommand, object?>
{
    private readonly IAgentMemoryRepository _memoryRepository;

    public ToggleMemoryPinCommandHandler(IAgentMemoryRepository memoryRepository)
    {
        _memoryRepository = memoryRepository;
    }

    public async Task<object?> Handle(ToggleMemoryPinCommand request, CancellationToken cancellationToken)
    {
        var memory = await _memoryRepository.GetByIdAsync(request.MemoryId, cancellationToken);
        if (memory == null || memory.AgentId != request.AgentId) return null;

        memory.IsPinned = !memory.IsPinned;
        await _memoryRepository.UpdateAsync(memory, cancellationToken);
        return new { memory.Id, memory.IsPinned };
    }
}
