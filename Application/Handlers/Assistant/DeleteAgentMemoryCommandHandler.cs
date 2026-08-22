// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Deletes one agent memory after checking that the caller owns it. A foreign personal memory and a
/// shared memory deleted by a non-administrator both answer like a missing memory, so the endpoint
/// never confirms that a foreign memory exists.
/// </summary>
/// <param name="memoryRepository">Loads and removes the memory</param>
/// <param name="userService">Resolves the calling user and their administrator role</param>

using Klacks.Api.Application.Commands.Assistant;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Services.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Assistant;

public class DeleteAgentMemoryCommandHandler : IRequestHandler<DeleteAgentMemoryCommand, Unit>
{
    private readonly IAgentMemoryRepository _memoryRepository;
    private readonly IUserService _userService;

    public DeleteAgentMemoryCommandHandler(IAgentMemoryRepository memoryRepository, IUserService userService)
    {
        _memoryRepository = memoryRepository;
        _userService = userService;
    }

    public async Task<Unit> Handle(DeleteAgentMemoryCommand request, CancellationToken cancellationToken)
    {
        var memory = await _memoryRepository.GetByIdAsync(request.MemoryId, cancellationToken);
        if (memory == null || memory.AgentId != request.AgentId)
        {
            throw new KeyNotFoundException($"Memory {request.MemoryId} not found for agent {request.AgentId}");
        }

        if (!AgentMemoryAccessPolicy.CanWrite(memory, _userService.GetId(), await _userService.IsAdmin()))
        {
            throw new KeyNotFoundException($"Memory {request.MemoryId} not found for agent {request.AgentId}");
        }

        await _memoryRepository.DeleteAsync(request.MemoryId, cancellationToken);
        return Unit.Value;
    }
}
