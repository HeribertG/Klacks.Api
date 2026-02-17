using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

public class DeleteAiMemorySkill : BaseSkill
{
    private readonly IAiMemoryRepository _aiMemoryRepository;

    public override string Name => "delete_ai_memory";

    public override string Description =>
        "Deletes a persistent memory entry by its ID. " +
        "This permanently removes the memory from the AI assistant's knowledge base.";

    public override SkillCategory Category => SkillCategory.Crud;

    public override IReadOnlyList<string> RequiredPermissions => new[] { "Admin" };

    public override IReadOnlyList<SkillParameter> Parameters => new[]
    {
        new SkillParameter(
            "memoryId",
            "The ID of the memory entry to delete.",
            SkillParameterType.String,
            Required: true)
    };

    public DeleteAiMemorySkill(IAiMemoryRepository aiMemoryRepository)
    {
        _aiMemoryRepository = aiMemoryRepository;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var memoryId = GetRequiredGuid(parameters, "memoryId");

        var memory = await _aiMemoryRepository.GetByIdAsync(memoryId, cancellationToken);
        if (memory == null)
        {
            return SkillResult.Error($"Memory with ID '{memoryId}' not found.");
        }

        var deletedKey = memory.Key;
        await _aiMemoryRepository.DeleteAsync(memoryId, cancellationToken);

        return SkillResult.SuccessResult(
            new { DeletedId = memoryId, DeletedKey = deletedKey },
            $"Memory '{deletedKey}' deleted.");
    }
}
