// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Creates an agent memory and stamps its owner: a personal category belongs to the calling user,
/// every other category becomes shared company-wide knowledge. Without the stamp every memory created
/// over the REST path would be company-wide and reach the prompt of every other user.
/// </summary>
/// <param name="memoryRepository">Persists the new memory</param>
/// <param name="embeddingService">Generates the semantic embedding when configured</param>
/// <param name="userService">Resolves the calling user from the authenticated principal</param>

using Klacks.Api.Application.Commands.Assistant;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Assistant;

public class CreateAgentMemoryCommandHandler : IRequestHandler<CreateAgentMemoryCommand, object>
{
    private readonly IAgentMemoryRepository _memoryRepository;
    private readonly IEmbeddingService _embeddingService;
    private readonly IUserService _userService;

    private const int DefaultImportance = 5;
    private const int MinImportance = 1;
    private const int MaxImportance = 10;

    public CreateAgentMemoryCommandHandler(
        IAgentMemoryRepository memoryRepository,
        IEmbeddingService embeddingService,
        IUserService userService)
    {
        _memoryRepository = memoryRepository;
        _embeddingService = embeddingService;
        _userService = userService;
    }

    public async Task<object> Handle(CreateAgentMemoryCommand request, CancellationToken cancellationToken)
    {
        var category = request.Category ?? MemoryCategories.Fact;

        var memory = new AgentMemory
        {
            AgentId = request.AgentId,
            UserId = AgentMemoryAccessPolicy.ResolveOwner(category, _userService.GetId()),
            Category = category,
            Key = request.Key,
            Content = request.Content,
            Importance = Math.Clamp(request.Importance ?? DefaultImportance, MinImportance, MaxImportance),
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
