using Klacks.Api.Application.Queries.Assistant;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Assistant;

public class GetAgentMemoriesQueryHandler : IRequestHandler<GetAgentMemoriesQuery, object>
{
    private readonly IAgentMemoryRepository _memoryRepository;
    private readonly IEmbeddingService _embeddingService;

    public GetAgentMemoriesQueryHandler(
        IAgentMemoryRepository memoryRepository,
        IEmbeddingService embeddingService)
    {
        _memoryRepository = memoryRepository;
        _embeddingService = embeddingService;
    }

    public async Task<object> Handle(GetAgentMemoriesQuery request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var queryEmbedding = _embeddingService.IsAvailable
                ? await _embeddingService.GenerateEmbeddingAsync(request.Search, cancellationToken)
                : null;

            var results = await _memoryRepository.HybridSearchAsync(
                request.AgentId, request.Search, queryEmbedding, 20, cancellationToken);
            return results;
        }

        if (!string.IsNullOrWhiteSpace(request.Category))
        {
            var memories = await _memoryRepository.GetByCategoryAsync(
                request.AgentId, request.Category, cancellationToken);
            return memories.Select(MapMemory).ToList();
        }

        var all = await _memoryRepository.GetAllAsync(request.AgentId, cancellationToken);
        return all.Select(MapMemory).ToList();
    }

    private static object MapMemory(AgentMemory m) => new
    {
        m.Id, m.Key, m.Content, m.Category, m.Importance,
        m.IsPinned, m.Source, m.ExpiresAt, m.AccessCount, m.CreateTime
    };
}
