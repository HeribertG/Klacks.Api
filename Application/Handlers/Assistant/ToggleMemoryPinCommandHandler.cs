// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Toggles the pin flag of one agent memory after checking that the caller owns it. Pinning puts a
/// memory into every turn of its audience, so a foreign personal memory and a shared memory pinned by
/// a non-administrator both answer like a missing memory.
/// </summary>
/// <param name="memoryRepository">Loads and persists the memory</param>
/// <param name="userService">Resolves the calling user and their administrator role</param>

using Klacks.Api.Application.Commands.Assistant;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Services.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Assistant;

public class ToggleMemoryPinCommandHandler : IRequestHandler<ToggleMemoryPinCommand, object?>
{
    private readonly IAgentMemoryRepository _memoryRepository;
    private readonly IUserService _userService;

    public ToggleMemoryPinCommandHandler(IAgentMemoryRepository memoryRepository, IUserService userService)
    {
        _memoryRepository = memoryRepository;
        _userService = userService;
    }

    public async Task<object?> Handle(ToggleMemoryPinCommand request, CancellationToken cancellationToken)
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

        memory.IsPinned = !memory.IsPinned;
        await _memoryRepository.UpdateAsync(memory, cancellationToken);
        return new { memory.Id, memory.IsPinned };
    }
}
