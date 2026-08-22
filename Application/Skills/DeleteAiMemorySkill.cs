// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Skill that removes one persistent memory entry by its identifier. Who may remove it is decided by
/// AgentMemoryAccessPolicy, the same rule the REST path applies: a shared company memory only by an
/// administrator, a personal memory only by the user it belongs to — an administrator included. A
/// denied delete answers like a missing memory so the skill never confirms that a foreign memory exists.
/// </summary>
/// <param name="agentMemoryRepository">Loads the memory and removes it</param>

using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("delete_ai_memory")]
public class DeleteAiMemorySkill : BaseSkillImplementation
{
    private const string MemoryIdParameter = "memoryId";

    private readonly IAgentMemoryRepository _agentMemoryRepository;

    public DeleteAiMemorySkill(IAgentMemoryRepository agentMemoryRepository)
    {
        _agentMemoryRepository = agentMemoryRepository;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var memoryId = GetRequiredGuid(parameters, MemoryIdParameter);

        var memory = await _agentMemoryRepository.GetByIdAsync(memoryId, cancellationToken);
        if (memory == null ||
            !AgentMemoryAccessPolicy.CanWrite(memory, context.UserId, context.UserPermissions.Contains(Roles.Admin)))
        {
            return SkillResult.Error($"Memory with ID '{memoryId}' not found.");
        }

        var deletedKey = memory.Key;
        await _agentMemoryRepository.DeleteAsync(memoryId, cancellationToken);

        return SkillResult.SuccessResult(
            new { DeletedId = memoryId, DeletedKey = deletedKey },
            $"Memory '{deletedKey}' deleted.");
    }
}
