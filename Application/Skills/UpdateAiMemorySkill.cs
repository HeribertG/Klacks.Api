using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.AI;
using Klacks.Api.Domain.Models.Skills;
using Klacks.Api.Domain.Services.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

public class UpdateAiMemorySkill : BaseSkill
{
    private readonly IAiMemoryRepository _aiMemoryRepository;

    public override string Name => "update_ai_memory";

    public override string Description =>
        "Updates an existing persistent memory entry. " +
        "You can update the key, content, category, or importance of a specific memory.";

    public override SkillCategory Category => SkillCategory.Crud;

    public override IReadOnlyList<string> RequiredPermissions => new[] { "Admin" };

    public override IReadOnlyList<SkillParameter> Parameters => new[]
    {
        new SkillParameter(
            "memoryId",
            "The ID of the memory entry to update.",
            SkillParameterType.String,
            Required: true),
        new SkillParameter(
            "key",
            "New short identifier or title for the memory entry.",
            SkillParameterType.String,
            Required: false),
        new SkillParameter(
            "content",
            "New memory content.",
            SkillParameterType.String,
            Required: false),
        new SkillParameter(
            "category",
            "New category. Options: user_preference, system_knowledge, learned_fact, workflow, context.",
            SkillParameterType.String,
            Required: false),
        new SkillParameter(
            "importance",
            "New importance level from 1 (low) to 10 (high).",
            SkillParameterType.Integer,
            Required: false)
    };

    public UpdateAiMemorySkill(IAiMemoryRepository aiMemoryRepository)
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

        var key = GetParameter<string>(parameters, "key");
        var content = GetParameter<string>(parameters, "content");
        var category = GetParameter<string>(parameters, "category");
        var importance = GetParameter<int?>(parameters, "importance");

        if (!string.IsNullOrWhiteSpace(key))
            memory.Key = key;

        if (!string.IsNullOrWhiteSpace(content))
            memory.Content = content;

        if (!string.IsNullOrWhiteSpace(category))
            memory.Category = category;

        if (importance.HasValue)
            memory.Importance = Math.Clamp(importance.Value, 1, 10);

        await _aiMemoryRepository.UpdateAsync(memory, cancellationToken);

        return SkillResult.SuccessResult(
            new { memory.Id, memory.Key, memory.Content, memory.Category, memory.Importance },
            $"Memory '{memory.Key}' updated.");
    }
}
