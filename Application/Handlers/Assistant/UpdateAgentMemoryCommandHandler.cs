// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Commands.Assistant;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Assistant;

public class UpdateAgentMemoryCommandHandler : IRequestHandler<UpdateAgentMemoryCommand, object?>
{
    private readonly IAgentMemoryRepository _memoryRepository;
    private readonly IEmbeddingService _embeddingService;

    public UpdateAgentMemoryCommandHandler(
        IAgentMemoryRepository memoryRepository,
        IEmbeddingService embeddingService)
    {
        _memoryRepository = memoryRepository;
        _embeddingService = embeddingService;
    }

    public async Task<object?> Handle(UpdateAgentMemoryCommand request, CancellationToken cancellationToken)
    {
        var memory = await _memoryRepository.GetByIdAsync(request.MemoryId, cancellationToken);
        if (memory == null || memory.AgentId != request.AgentId) return null;

        var contentChanged = false;
        if (request.Key != null) { memory.Key = request.Key; contentChanged = true; }
        if (request.Content != null) { memory.Content = request.Content; contentChanged = true; }
        if (request.Category != null) memory.Category = request.Category;
        if (request.Importance.HasValue) memory.Importance = Math.Clamp(request.Importance.Value, 1, 10);
        if (request.IsPinned.HasValue) memory.IsPinned = request.IsPinned.Value;

        if (contentChanged && _embeddingService.IsAvailable)
        {
            memory.Embedding = await _embeddingService.GenerateEmbeddingAsync(
                $"{memory.Key}: {memory.Content}", cancellationToken);
        }

        await _memoryRepository.UpdateAsync(memory, cancellationToken);

        return new
        {
            memory.Id, memory.Key, memory.Content, memory.Category, memory.Importance,
            memory.IsPinned, memory.Source, memory.ExpiresAt, memory.AccessCount, memory.CreateTime
        };
    }
}
