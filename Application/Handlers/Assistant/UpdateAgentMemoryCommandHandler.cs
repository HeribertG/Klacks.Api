// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Updates one agent memory after checking that the caller owns it. A foreign personal memory and a
/// shared memory touched by a non-administrator both answer like a missing memory, so the endpoint
/// never confirms that a foreign memory exists.
/// </summary>
/// <param name="memoryRepository">Loads and persists the memory</param>
/// <param name="embeddingService">Regenerates the embedding when key or content changed</param>
/// <param name="userService">Resolves the calling user and their administrator role</param>

using Klacks.Api.Application.Commands.Assistant;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Services.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Assistant;

public class UpdateAgentMemoryCommandHandler : IRequestHandler<UpdateAgentMemoryCommand, object?>
{
    private readonly IAgentMemoryRepository _memoryRepository;
    private readonly IEmbeddingService _embeddingService;
    private readonly IUserService _userService;

    private const int MinImportance = 1;
    private const int MaxImportance = 10;

    public UpdateAgentMemoryCommandHandler(
        IAgentMemoryRepository memoryRepository,
        IEmbeddingService embeddingService,
        IUserService userService)
    {
        _memoryRepository = memoryRepository;
        _embeddingService = embeddingService;
        _userService = userService;
    }

    public async Task<object?> Handle(UpdateAgentMemoryCommand request, CancellationToken cancellationToken)
    {
        var memory = await _memoryRepository.GetByIdAsync(request.MemoryId, cancellationToken);
        if (memory == null || memory.AgentId != request.AgentId)
        {
            return null;
        }

        if (!AgentMemoryAccessPolicy.CanWrite(memory, _userService.GetId(), await _userService.IsAdmin()))
        {
            return null;
        }

        var contentChanged = false;
        if (request.Key != null) { memory.Key = request.Key; contentChanged = true; }
        if (request.Content != null) { memory.Content = request.Content; contentChanged = true; }
        if (request.Category != null) memory.Category = request.Category;
        if (request.Importance.HasValue) memory.Importance = Math.Clamp(request.Importance.Value, MinImportance, MaxImportance);
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
