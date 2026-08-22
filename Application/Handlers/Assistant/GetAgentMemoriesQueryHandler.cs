// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Returns the agent memories the calling user is allowed to see: shared company-wide memories plus
/// their own personal ones. The scope comes from the authenticated principal, never from the request,
/// so no caller can widen it.
/// </summary>
/// <param name="memoryRepository">Source of the memories, scoped by the owning user</param>
/// <param name="embeddingService">Builds the query embedding for the semantic search branch</param>
/// <param name="userService">Resolves the calling user from the authenticated principal</param>

using Klacks.Api.Application.Queries.Assistant;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Assistant;

public class GetAgentMemoriesQueryHandler : IRequestHandler<GetAgentMemoriesQuery, object>
{
    private readonly IAgentMemoryRepository _memoryRepository;
    private readonly IEmbeddingService _embeddingService;
    private readonly IUserService _userService;

    public GetAgentMemoriesQueryHandler(
        IAgentMemoryRepository memoryRepository,
        IEmbeddingService embeddingService,
        IUserService userService)
    {
        _memoryRepository = memoryRepository;
        _embeddingService = embeddingService;
        _userService = userService;
    }

    public async Task<object> Handle(GetAgentMemoriesQuery request, CancellationToken cancellationToken)
    {
        var currentUserId = _userService.GetId();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var queryEmbedding = _embeddingService.IsAvailable
                ? await _embeddingService.GenerateEmbeddingAsync(request.Search, cancellationToken)
                : null;

            var results = await _memoryRepository.HybridSearchAsync(
                request.AgentId, request.Search, queryEmbedding, SearchResultLimit, currentUserId, cancellationToken);
            return results;
        }

        if (!string.IsNullOrWhiteSpace(request.Category))
        {
            var memories = await _memoryRepository.GetByCategoryAsync(
                request.AgentId, request.Category, currentUserId, cancellationToken);
            return memories.Select(MapMemory).ToList();
        }

        var all = await _memoryRepository.GetAllAsync(request.AgentId, currentUserId, cancellationToken);
        return all.Select(MapMemory).ToList();
    }

    private const int SearchResultLimit = 20;

    private static object MapMemory(AgentMemory m) => new
    {
        m.Id, m.Key, m.Content, m.Category, m.Importance,
        m.IsPinned, m.Source, m.ExpiresAt, m.AccessCount, m.CreateTime
    };
}
