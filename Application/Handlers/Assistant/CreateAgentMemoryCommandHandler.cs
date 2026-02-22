using Klacks.Api.Application.Commands.Assistant;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Assistant;

public class CreateAgentMemoryCommandHandler : IRequestHandler<CreateAgentMemoryCommand, object>
{
    private readonly IAgentMemoryRepository _memoryRepository;
    private readonly IEmbeddingService _embeddingService;

    public CreateAgentMemoryCommandHandler(
        IAgentMemoryRepository memoryRepository,
        IEmbeddingService embeddingService)
    {
        _memoryRepository = memoryRepository;
        _embeddingService = embeddingService;
    }

    public async Task<object> Handle(CreateAgentMemoryCommand request, CancellationToken cancellationToken)
    {
        var memory = new AgentMemory
        {
            AgentId = request.AgentId,
            Category = request.Category ?? MemoryCategories.Fact,
            Key = request.Key,
            Content = request.Content,
            Importance = Math.Clamp(request.Importance ?? 5, 1, 10),
            IsPinned = request.IsPinned ?? false,
            Source = MemorySources.UserExplicit,
            ExpiresAt = request.ExpiresAt
        };

        if (_embeddingService.IsAvailable)
        {
            memory.Embedding = await _embeddingService.GenerateEmbeddingAsync(
                $"{memory.Key}: {memory.Content}", cancellationToken);
        }

        await _memoryRepository.AddAsync(memory, cancellationToken);

        return new
        {
            memory.Id, memory.Key, memory.Content, memory.Category, memory.Importance,
            memory.IsPinned, memory.Source, memory.ExpiresAt, memory.AccessCount, memory.CreateTime
        };
    }
}
